using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.News;

/// <summary>
/// Заходит на страницы новостей, которые ещё не обрабатывали: берёт главное фото (og:image) и полный текст.
/// Лента RSS часто даёт только заголовок и пару строк, а у агрегаторов (Bing, Google News) нет ни фото, ни текста.
/// </summary>
public sealed class NewsEnrichmentService(
    IServiceScopeFactory scopes,
    IHttpClientFactory httpFactory,
    IOptions<NewsOptions> options,
    ILogger<NewsEnrichmentService> log) : BackgroundService
{
    public const string HttpClientName = "news-article";

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = options.Value;
        if (!o.Enabled || !o.EnrichEnabled)
        {
            log.LogInformation("Загрузка фото и текста новостей выключена");
            return;
        }

        try
        {
            // Даём первому опросу лент положить новости в базу.
            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(o.EnrichIntervalMinutes));
            do
            {
                try { await EnrichBatchAsync(ct); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Ошибка загрузки фото и текста новостей");
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }

    private async Task EnrichBatchAsync(CancellationToken ct)
    {
        var o = options.Value;

        List<(long Id, string Url)> batch;
        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var since = DateTimeOffset.UtcNow.AddDays(-o.EnrichWindowDays);
            var rows = await db.News.AsNoTracking()
                .Where(n => n.EnrichedAt == null && n.PublishedAt >= since)
                .OrderByDescending(n => n.PublishedAt)
                .Select(n => new { n.Id, n.Url })
                .Take(o.EnrichBatchSize)
                .ToListAsync(ct);
            batch = rows.Select(r => (r.Id, r.Url)).ToList();
        }

        if (batch.Count == 0) return;

        var client = httpFactory.CreateClient(HttpClientName);
        var results = new ConcurrentDictionary<long, ArticlePage?>();

        // Несколько страниц параллельно, но немного: это чужие сайты.
        await Parallel.ForEachAsync(batch, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (item, token) => results[item.Id] = await FetchAsync(client, item.Url, o.ContentMaxLength, token));

        using (var scope = scopes.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ids = results.Keys.ToList();
            var items = await db.News.Where(n => ids.Contains(n.Id)).ToListAsync(ct);
            var now = DateTimeOffset.UtcNow;

            foreach (var item in items)
            {
                var page = results[item.Id];
                item.EnrichedAt = now;            // и при неудаче: иначе одна и та же битая страница бесконечно попадала бы в очередь
                if (page is null) continue;

                item.Content = page.Text;
                if (page.ImageUrl is not null) item.ImageUrl = page.ImageUrl;   // фото со страницы лучше превью из ленты
            }

            await db.SaveChangesAsync(ct);
            log.LogInformation("Обработано страниц новостей: {Count}, с текстом: {WithText}, с фото: {WithImage}",
                items.Count, results.Values.Count(r => r?.Text is not null), results.Values.Count(r => r?.ImageUrl is not null));
        }
    }

    private async Task<ArticlePage?> FetchAsync(HttpClient client, string url, int maxTextLength, CancellationToken ct)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

            // Google News отдаёт не статью, а свою страницу-заглушку: фото и текст оттуда были бы чужими.
            if (uri.Host.EndsWith("news.google.com", StringComparison.OrdinalIgnoreCase)) return null;

            using var resp = await client.GetAsync(uri, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var type = resp.Content.Headers.ContentType?.MediaType;
            if (type is not ("text/html" or "application/xhtml+xml")) return null;

            var html = await resp.Content.ReadAsStringAsync(ct);
            return ArticleExtractor.Parse(html, resp.RequestMessage?.RequestUri ?? uri, maxTextLength);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Таймаут, закрытый сайт, не публичный адрес и т. п.: для ленты это не ошибка.
            log.LogDebug("Не удалось получить страницу {Url}", url);
            return null;
        }
    }
}
