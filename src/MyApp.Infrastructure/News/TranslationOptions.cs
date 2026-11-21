using System.ComponentModel.DataAnnotations;

namespace MyApp.Infrastructure.News;

/// <summary>Секция "Translation". Ключ задаётся только переменной окружения Translation__ApiKey.</summary>
public sealed class TranslationOptions
{
    public const string SectionName = "Translation";

    /// <summary>false: кнопка «Показать перевод» отвечает «перевод недоступен».</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Адрес Messages API (Anthropic или ваш шлюз, как у AiChat).</summary>
    [Required]
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";

    public string ApiKey { get; set; } = "";

    /// <summary>false: заголовок x-api-key (Anthropic). true: Authorization: Bearer (если так требует шлюз).</summary>
    public bool UseBearerAuth { get; set; }

    /// <summary>Недорогая быстрая модель; для шлюза укажите имя, которое он ожидает.</summary>
    [Required]
    public string Model { get; set; } = "claude-haiku-5-5";

    [Required, StringLength(8, MinimumLength = 2)]
    public string TargetLang { get; set; } = "ru";

    /// <summary>Сколько новых переводов можно создать за сутки (всего, а не на пользователя).</summary>
    [Range(1, 100000)]
    public int DailyLimit { get; set; } = 300;

    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}
