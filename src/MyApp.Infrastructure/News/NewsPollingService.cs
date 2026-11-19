using System.Net;
using System.Xml;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.News;
using MyApp.Infrastructure.Persistence;
using System.ServiceModel.Syndication;

namespace MyApp.Infrastructure.News;

public sealed class NewsPollingService(
    IServiceScopeFactory scopes,
    IHttpClientFactory httpFactory,
    IOptions<NewsOptions> options,
    ILogger<NewsPollingService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);   // например 10 минут
        do
        {
            try { await PollAllAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { log.LogError(ex, "Ошибка цикла опроса новостей"); }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task PollAllAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sources = await db.NewsSources.Where(s => s.IsEnabled).ToListAsync(ct);

        foreach (var src in sources)
        {
            try { await PollSourceAsync(db, src, ct); src.LastError = null; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                src.LastError = ex.Message;
                log.LogWarning(ex, "Не удалось опросить {Source}", src.Name);
            }
            src.LastFetchedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task PollSourceAsync(AppDbContext db, NewsSource src, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("news");
        using var req = new HttpRequestMessage(HttpMethod.Get, src.FeedUrl);
        if (src.ETag is not null) req.Headers.IfNoneMatch.ParseAdd(src.ETag);

        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (resp.StatusCode == HttpStatusCode.NotModified) return;
        resp.EnsureSuccessStatusCode();
        src.ETag = resp.Headers.ETag?.ToString();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var xml = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,   // защита от XXE
            Async = true
        });
        var feed = SyndicationFeed.Load(xml);          // пакет System.ServiceModel.Syndication

        var items = feed.Items
            .Select(i => new
            {
                Id = string.IsNullOrEmpty(i.Id) ? i.Links.FirstOrDefault()?.Uri.ToString() : i.Id,
                Item = i
            })
            .Where(x => x.Id is not null)
            .ToList();

        var ids = items.Select(x => x.Id!).ToList();
        var existing = await db.NewsItems
            .Where(n => n.SourceId == src.Id && ids.Contains(n.ExternalId))
            .Select(n => n.ExternalId).ToHashSetAsync(ct);

        foreach (var x in items.Where(x => !existing.Contains(x.Id!)))
        {
            db.NewsItems.Add(new NewsItem
            {
                SourceId = src.Id,
                ExternalId = x.Id!,
                Title = x.Item.Title?.Text?.Trim() ?? "(без заголовка)",
                Url = x.Item.Links.FirstOrDefault()?.Uri.ToString() ?? src.FeedUrl,
                Summary = Sanitize(x.Item.Summary?.Text),     // вырезать HTML, обрезать до ~500 символов
                PublishedAt = x.Item.PublishDate != default ? x.Item.PublishDate : DateTimeOffset.UtcNow,
                FetchedAt = DateTimeOffset.UtcNow
            });
        }
    }
    private string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var text = WebUtility.HtmlDecode(doc.DocumentNode.InnerText).Trim();
        return text.Length > 500
            ? text[..500]
            : text;
    }
}