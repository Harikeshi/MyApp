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

    // Манул (кошка Палласа): названия на английском и русские падежные формы. Ловим целые слова (см. KeywordFilter).
    private static readonly string[] ManulKeywords =
    [
        "Pallas’ cat", "Pallas' cat", "Pallas’ cats", "Pallas' cats",
        "manul", "manuls", "Pallas's cat", "Pallas’s cat", "Pallas's cats", "Pallas’s cats", "Pallas cat", "Pallas cats",
        "Otocolobus", "манул", "манула", "манулу", "манулом", "мануле", "манулы", "манулов", "манулам", "манулами", "манулах",
        // другие языки; японский и китайский ищутся как подстрока (см. KeywordFilter)
        "Pallaskatze", "Pallas-Katze", "chat de Pallas", "gatto di Pallas",
        "Pallas-cat",
        "Otocolobus manul",
        "manul cat",
        "steppe cat",
        "兔狲",        // китайский
        "マヌルネコ",   // японский
        "манул-кот",
        "палласов кот",
        "манулёнок",
        "манулята",
        "манулята",
        "палласов кот",
        "палласов кота"
    ];

    private static readonly NewsSource[] Defaults =
    [
        new()
        {
            Name = "Google News: Otocolobus manul",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://news.google.com/rss/search?q=%22Otocolobus+manul%22"
        },

        new()
        {
            Name = "Bing News: Pallas cat",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=Pallas+cat&format=rss"
        },

        new()
        {
            Name = "Bing News: Otocolobus manul",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=Otocolobus+manul&format=rss"
        },
        new()
        {
            Name = "Yahoo News: Pallas cat",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://news.search.yahoo.com/rss?p=Pallas+cat"
        },
        new()
        {
            Name = "Reddit: manul",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.reddit.com/search.rss?q=manul&sort=new"
        },
        new()
        {
            Name = "Reddit: Pallas cat",
            Topic = NewsTopic.Manuls,
            IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.reddit.com/search.rss?q=%22Pallas+cat%22&sort=new"
        },
        // Крипта: ленты целиком про тему, фильтр не нужен.
        new() { Name = "Cointelegraph", Topic = NewsTopic.Crypto, FeedUrl = "https://cointelegraph.com/rss" },
        new() { Name = "CoinDesk",      Topic = NewsTopic.Crypto, FeedUrl = "https://www.coindesk.com/arc/outboundfeeds/rss/?outputType=xml" },
        new() { Name = "Decrypt",       Topic = NewsTopic.Crypto, FeedUrl = "https://decrypt.co/feed" },

        // Рынки: общие ленты, поэтому с фильтром по ключевым словам.
        new() { Name = "MarketWatch",   Topic = NewsTopic.Markets, FeedUrl = "https://feeds.marketwatch.com/marketwatch/topstories", IncludeKeywords = MarketKeywords },
        new() { Name = "CNBC",          Topic = NewsTopic.Markets, FeedUrl = "https://www.cnbc.com/id/100003114/device/rss/rss.html", IncludeKeywords = MarketKeywords },

        // Манулы: поисковые ленты Bing News. В отличие от Google News, в них есть превью и прямая ссылка на статью
        // (ссылка Google News ведёт на страницу-заглушку, с которой нельзя взять ни фото, ни текст).
        new() { Name = "Bing News: Pallas's cat (en)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
                FeedUrl = "https://www.bing.com/news/search?q=%22Pallas%27s%20cat%22%20OR%20manul&setmkt=en-US&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: манул (ru)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
                FeedUrl = "https://www.bing.com/news/search?q=%D0%BC%D0%B0%D0%BD%D1%83%D0%BB&setmkt=ru-RU&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: Pallaskatze (de)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
                FeedUrl = "https://www.bing.com/news/search?q=Pallaskatze%20OR%20Manul&setmkt=de-DE&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: chat de Pallas (fr)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
                FeedUrl = "https://www.bing.com/news/search?q=%22chat%20de%20Pallas%22%20OR%20manul&setmkt=fr-FR&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: gato de Pallas (es)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
                FeedUrl = "https://www.bing.com/news/search?q=%22gato%20de%20Pallas%22%20OR%20manul&setmkt=es-ES&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: 兔猻 (zh-TW)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=%E5%85%94%E7%8C%BB&setmkt=zh-TW&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: палласов кот / манул зоопарк (ru)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=%22%D0%BF%D0%B0%D0%BB%D0%BB%D0%B0%D1%81%D0%BE%D0%B2%20%D0%BA%D0%BE%D1%82%22%20OR%20%22%D0%BC%D0%B0%D0%BD%D1%83%D0%BB%20%D0%B7%D0%BE%D0%BE%D0%BF%D0%B0%D1%80%D0%BA%22%20OR%20%22%D0%BA%D0%BE%D1%82%D1%8F%D1%82%D0%B0%20%D0%BC%D0%B0%D0%BD%D1%83%D0%BB%D0%B0%22&setmkt=ru-RU&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: gatto di Pallas (it)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=%22gatto%20di%20Pallas%22%20OR%20manul&setmkt=it-IT&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: kot Pallasa (pl)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=manul%20OR%20%22kot%20Pallasa%22&setmkt=pl-PL&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: Pallas kedisi (tr)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=%22Pallas%20kedisi%22%20OR%20manul&setmkt=tr-TR&format=rss&qft=interval%3d%229%22" },
        new() { Name = "Bing News: gato-de-Pallas (pt)", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords,
            FeedUrl = "https://www.bing.com/news/search?q=%22gato-de-Pallas%22%20OR%20%22gato%20de%20Pallas%22%20OR%20manul&setmkt=pt-BR&format=rss&qft=interval%3d%229%22" },
        // Россия: общие ленты, берём только записи с ключевыми словами манула
        new() { Name = "Lenta.ru",                 Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://lenta.ru/rss" },
        new() { Name = "РИА Новости",              Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://ria.ru/export/rss2/archive/index.xml" },
        new() { Name = "ТАСС",                     Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://tass.ru/rss/v2.xml" },
        new() { Name = "Известия",                 Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://iz.ru/xml/rss/all.xml" },
        new() { Name = "Московский комсомолец",    Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.mk.ru/rss/index.xml" },
        new() { Name = "Комсомольская правда",     Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://kp.ru/rss/allsections.xml" },
        new() { Name = "Коммерсантъ: наука",       Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.kommersant.ru/RSS/science.xml" },
        // по памяти, не из списков выше:
        new() { Name = "N+1",                      Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://nplus1.ru/rss" },
        new() { Name = "Naked Science",            Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://naked-science.ru/feed" },
        
        // Природа и наука, английский (адреса по памяти, проверьте curl-ом)
        new() { Name = "Mongabay",              Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://news.mongabay.com/feed/" },
        new() { Name = "BBC Science & Environment", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://feeds.bbci.co.uk/news/science_and_environment/rss.xml" },
        new() { Name = "NHK 科学・文化",        Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.nhk.or.jp/rss/news/cat3.xml" },
        new() { Name = "The Guardian: Environment", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.theguardian.com/environment/rss" },
        new() { Name = "Edinburgh Zoo",   Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.edinburghzoo.org.uk/rss.xml" },
        new() { Name = "Great Plains Zoo", Topic = NewsTopic.Manuls, IncludeKeywords = ManulKeywords, FeedUrl = "https://www.greatzoo.org/feed/" },
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
