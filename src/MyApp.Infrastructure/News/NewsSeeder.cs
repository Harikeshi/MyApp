using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Domain.News;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.News;

/// <summary>Добавляет стартовые источники, которых ещё нет (по FeedUrl). Регистрируется раньше воркеров.</summary>
public sealed class NewsSeeder(
    IServiceScopeFactory scopes,
    IOptions<NewsOptions> options,
    ILogger<NewsSeeder> log) : IHostedService
{
    private static readonly string[] MarketKeywords =
    [
        "stock", "stocks", "shares", "market", "markets", "Wall Street", "Fed", "Treasury", "yield", "yields",
        "bond", "bonds", "inflation", "earnings", "S&P", "Nasdaq", "Dow", "oil", "dollar", "rates", "ECB",
        "IPO", "ETF", "rally", "selloff", "recession", "GDP"
    ];

    private static readonly NewsSource[] Defaults =
    [
        // Крипта: ленты целиком про тему, фильтр не нужен.
        new() { Name = "Cointelegraph", Topic = NewsTopic.Crypto, FeedUrl = "https://cointelegraph.com/rss" },
        new() { Name = "CoinDesk",      Topic = NewsTopic.Crypto, FeedUrl = "https://www.coindesk.com/arc/outboundfeeds/rss/?outputType=xml" },
        new() { Name = "Decrypt",       Topic = NewsTopic.Crypto, FeedUrl = "https://decrypt.co/feed" },

        // Рынки: общие ленты, поэтому с фильтром по ключевым словам.
        new() { Name = "MarketWatch",   Topic = NewsTopic.Markets, FeedUrl = "https://feeds.marketwatch.com/marketwatch/topstories", IncludeKeywords = MarketKeywords },
        new() { Name = "CNBC",          Topic = NewsTopic.Markets, FeedUrl = "https://www.cnbc.com/id/100003114/device/rss/rss.html", IncludeKeywords = MarketKeywords },

        // Адрес не проверен на живой ленте, поэтому выключен: проверьте curl-ом и включите.
        new() { Name = "Bloomberg Markets", Topic = NewsTopic.Markets, FeedUrl = "https://feeds.bloomberg.com/markets/news.rss", IsEnabled = false },
    ];

    public async Task StartAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled || !options.Value.SeedDefaultSources) return;

        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var existing = (await db.NewsSources.Select(s => s.FeedUrl).ToListAsync(ct)).ToHashSet();
            var missing = Defaults.Where(d => !existing.Contains(d.FeedUrl)).ToList();
            if (missing.Count == 0) return;

            db.NewsSources.AddRange(missing);
            await db.SaveChangesAsync(ct);
            log.LogInformation("Добавлено стартовых источников новостей: {Count}", missing.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Например, миграция ещё не применена: приложение при этом всё равно должно стартовать.
            log.LogError(ex, "Не удалось добавить стартовые источники новостей");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
