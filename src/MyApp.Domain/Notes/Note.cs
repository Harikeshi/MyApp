namespace MyApp.Domain.Notes;

/// <summary>Запись пользователя. Содержимое хранится как HTML (текст с цветом, картинки data:-ссылками).</summary>
public class Note
{
    public const int MaxTitleLength = 200;
    public const int MaxContentLength = 10_000_000;

    private Note() { }                          // для EF Core

    public Note(string userId, string title, NoteTag tag, string content, DateTime nowUtc)
    {
        UserId = userId;
        Title = NormalizeTitle(title);
        Tag = tag;
        SetContent(content);
        CreatedAt = nowUtc;
        UpdatedAt = nowUtc;
    }

    public int Id { get; private set; }
    public string UserId { get; private set; } = "";
    public string Title { get; private set; } = "";
    public NoteTag Tag { get; private set; }
    public string Content { get; private set; } = "";

    /// <summary>Содержимое без разметки: для поиска и превью.</summary>
    public string PlainText { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void Update(string title, NoteTag tag, string content, DateTime nowUtc)
    {
        Title = NormalizeTitle(title);
        Tag = tag;
        SetContent(content);
        UpdatedAt = nowUtc;
    }

    private void SetContent(string? content)
    {
        var html = content ?? "";
        if (html.Length > MaxContentLength)
            throw new ArgumentException("Запись слишком большая", nameof(content));
        Content = html;
        PlainText = NoteText.ToPlain(html);
    }

    private static string NormalizeTitle(string? title)
    {
        var t = title?.Trim() ?? "";
        if (t.Length < 1 || t.Length > MaxTitleLength)
            throw new ArgumentException(
                $"Название должно содержать от 1 до {MaxTitleLength} символов", nameof(title));
        return t;
    }
}
