using MyApp.Application.News;

namespace MyApp.Application.Abstractions;

/// <summary>Порт переводчика новостей. Реализация (запрос к Claude) лежит в Infrastructure.</summary>
public interface INewsTranslator
{
    /// <summary>false: ключ не задан или перевод выключен настройкой.</summary>
    bool IsConfigured { get; }

    /// <summary>Язык, на который переводим ("ru").</summary>
    string TargetLang { get; }

    /// <summary>Имя модели (сохраняется вместе с переводом).</summary>
    string Model { get; }

    /// <summary>Сколько новых переводов разрешено за сутки (защита от лишних расходов).</summary>
    int DailyLimit { get; }

    /// <exception cref="TranslationFailedException">Сервис перевода недоступен или вернул непригодный ответ.</exception>
    Task<(string Title, string? Summary)> TranslateAsync(string title, string? summary, CancellationToken ct);
}
