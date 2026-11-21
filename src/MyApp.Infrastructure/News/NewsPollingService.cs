using System.Net;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Domain.News;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.News;

/// <summary>Раз в PollIntervalMinutes обходит включённые источники и сохраняет новые новости.</summary>
public sealed class NewsPollingService(
    IServiceScopeFactory scopes,
    IHttpClientFactory httpFactory,
    IOptions<NewsOptions> options,
    ILogger<NewsPollingService> log) : BackgroundService
{
    public const string HttpClientName = "news";
    private const string MediaRss = "http://search.yahoo.com/mrss/";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled)
        {
            log.LogInformation("Опрос новостей выключен (News:Enabled=false)");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.PollIntervalMinutes));
        try
        {
            do
            {
                try { await PollAllAsync(ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Ошибка цикла опроса новостей");
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task PollAllAsync(CancellationToken ct)
    {
        List<int> ids;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ids = await db.NewsSources.AsNoTracking().Where(s => s.IsEnabled).Select(s => s.Id).ToListAsync(ct);
        }

        // Отдельный scope (и DbContext) на источник: сбой одного не портит остальные.
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var src = await db.NewsSources.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (src is null) continue;

            var now = DateTimeOffset.UtcNow;
            try
            {
                var added = await PollSourceAsync(db, src, ct);
                src.LastSuccessAt = now;
                src.LastError = null;
                if (added > 0) log.LogInformation("{Source}: новых новостей {Count}", src.Name, added);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                src.LastError = TextCleaner.Truncate(ex.Message, 500);
                log.LogWarning(ex, "Не удалось опросить {Source}", src.Name);
            }
            src.LastFetchedAt = now;

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (NewsRepository.IsUniqueViolation(ex))
            {
                // Другой экземпляр приложения вставил те же новости. Не страшно: следующий цикл всё разрулит.
                db.ChangeTracker.Clear();
                await db.NewsSources.Where(s => s.Id == id).ExecuteUpdateAsync(
                    s => s.SetProperty(x => x.LastFetchedAt, now).SetProperty(x => x.LastSuccessAt, now), ct);
            }
        }
    }

    /// <summary>Добавляет новые новости в контекст (без SaveChanges) и возвращает их число.</summary>
    private async Task<int> PollSourceAsync(AppDbContext db, NewsSource src, CancellationToken ct)
    {
        var o = options.Value;
        var client = httpFactory.CreateClient(HttpClientName);

        using var req = new HttpRequestMessage(HttpMethod.Get, src.FeedUrl);
        if (!string.IsNullOrEmpty(src.ETag)) req.Headers.TryAddWithoutValidation("If-None-Match", src.ETag);

        using var resp = await client.SendAsync(req, ct);   // размер ответа ограничен MaxResponseContentBufferSize
        if (resp.StatusCode == HttpStatusCode.NotModified) return 0;
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var xml = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,   // защита от XXE
            XmlResolver = null
            // Async НЕ включаем: SyndicationFeed.Load читает синхронно, а на async-ридере это падает.
        });
        var feed = SyndicationFeed.Load(xml);

        var now = DateTimeOffset.UtcNow;
        var passes = KeywordFilter.Create(src.IncludeKeywords);
        var candidates = new List<NewsItem>();

        foreach (var it in feed.Items.Take(o.MaxItemsPerFeed))
        {
            var link = (it.Links.FirstOrDefault(l => l.RelationshipType is null or "alternate") ?? it.Links.FirstOrDefault())?.Uri;
            if (link is null || !link.IsAbsoluteUri || link.Scheme is not ("http" or "https")) continue;
            if (link.AbsoluteUri.Length > 2000) continue;

            var title = TextCleaner.Clean(it.Title?.Text, 300);
            if (title is null) continue;

            var rawSummary = it.Summary?.Text ?? (it.Content as TextSyndicationContent)?.Text;
            var fullText = TextCleaner.Clean(rawSummary, 5000);
            if (!passes(title, fullText)) continue;

            var externalId = string.IsNullOrWhiteSpace(it.Id) ? link.AbsoluteUri : it.Id.Trim();
            if (externalId.Length > 400)
                externalId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(externalId)));

            candidates.Add(new NewsItem
            {
                SourceId = src.Id,
                Topic = src.Topic,
                ExternalId = externalId,
                Title = title,
                Url = link.AbsoluteUri,
                Summary = fullText is null ? null : TextCleaner.Truncate(fullText, o.SummaryMaxLength),
                ImageUrl = ExtractImage(it),
                PublishedAt = NormalizeDate(it, now),
                FetchedAt = now
            });
        }

        candidates = candidates.DistinctBy(c => c.ExternalId).ToList();
        var externalIds = candidates.Select(c => c.ExternalId).ToList();

        var existing = (await db.News.AsNoTracking()
                .Where(n => n.SourceId == src.Id && externalIds.Contains(n.ExternalId))
                .Select(n => n.ExternalId)
                .ToListAsync(ct))
            .ToHashSet();

        var fresh = candidates.Where(c => !existing.Contains(c.ExternalId)).ToList();
        db.News.AddRange(fresh);

        src.ETag = resp.Headers.ETag?.ToString();   // запоминаем только после успешного разбора
        return fresh.Count;
    }

    /// <summary>Всегда UTC (иначе Npgsql бросит исключение), не из будущего, с точностью до секунды.</summary>
    private static DateTimeOffset NormalizeDate(SyndicationItem it, DateTimeOffset now)
    {
        var d = it.PublishDate != default ? it.PublishDate
              : it.LastUpdatedTime != default ? it.LastUpdatedTime
              : now;

        var utc = d.ToUniversalTime();
        if (utc > now.AddHours(1)) utc = now;
        return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
    }

    private static string? ExtractImage(SyndicationItem it)
    {
        var url = it.Links
            .FirstOrDefault(l => l.RelationshipType == "enclosure"
                              && l.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            ?.Uri?.ToString();

        if (url is null)
        {
            foreach (var ext in it.ElementExtensions)
            {
                if (ext.OuterNamespace != MediaRss) continue;
                if (ext.OuterName is not ("thumbnail" or "content")) continue;

                var el = ext.GetObject<XElement>();
                if (ext.OuterName == "content")
                {
                    var medium = (string?)el.Attribute("medium");
                    var type = (string?)el.Attribute("type");
                    if (medium != "image" && type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true) continue;
                }

                url = (string?)el.Attribute("url");
                if (url is not null) break;
            }
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var u)
               && u.Scheme == Uri.UriSchemeHttps
               && u.AbsoluteUri.Length <= 2000
            ? u.AbsoluteUri
            : null;
    }
}
