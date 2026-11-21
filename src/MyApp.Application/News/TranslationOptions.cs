using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.News;

/// <summary>Секция "Translation" в appsettings. Ключ — в переменной Translation__ApiKey.</summary>
public sealed class TranslationOptions
{
    public const string Section = "Translation";

    /// <summary>false — воркер перевода не работает, кнопка отвечает 503.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Адрес Messages API (или вашего шлюза), со слэшем на конце или без.</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";

    public string ApiKey { get; set; } = "";

    /// <summary>Имя модели; для шлюза подставьте то, которое он ожидает.</summary>
    [Required]
    public string Model { get; set; } = "claude-haiku-5-5";

    [Required, StringLength(8, MinimumLength = 2)]
    public string TargetLang { get; set; } = "ru";

    /// <summary>Потолок переводов за один цикл воркера (контроль расходов).</summary>
    [Range(1, 200)]
    public int MaxPerCycle { get; set; } = 20;

    [Range(10, 3600)]
    public int CycleSeconds { get; set; } = 60;

    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 3;

    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
