using System.ComponentModel.DataAnnotations;

namespace MyApp.Infrastructure.News;

/// <summary>Секция "News" (переменные окружения: News__PollIntervalMinutes и т.д.).</summary>
public sealed class NewsOptions
{
    public const string SectionName = "News";

    /// <summary>false: не опрашиваем ленты и не чистим базу (локальная разработка без интернета).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Добавить стартовые источники (рынки и крипта), которых ещё нет в базе.</summary>
    public bool SeedDefaultSources { get; set; } = true;

    [Range(1, 1440)]
    public int PollIntervalMinutes { get; set; } = 10;

    /// <summary>Новости старше этого срока удаляются, если под ними нет комментариев.</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 60;

    [Range(1, 500)]
    public int MaxItemsPerFeed { get; set; } = 100;

    /// <summary>Сколько символов описания хранить. Клиент сам сворачивает длинное описание до превью.</summary>
    [Range(100, 5000)]
    public int SummaryMaxLength { get; set; } = 5000;

    /// <summary>User-Agent бота; лучше с контактом: "MyAppNewsBot/1.0 (+https://ваш-домен)".</summary>
    [Required]
    public string UserAgent { get; set; } = "MyAppNewsBot/1.0";

    /// <summary>Если ни один источник не обновлялся дольше этого срока, /health/news сообщит о проблеме.</summary>
    [Range(5, 1440)]
    public int HealthMaxAgeMinutes { get; set; } = 45;
}
