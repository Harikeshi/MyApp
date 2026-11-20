namespace MyApp.Application.Abstractions;

public interface INewsTranslator
{
    Task<(string Title, string? Summary)> TranslateAsync(string title, string? summary, string targetLang, CancellationToken ct);
}