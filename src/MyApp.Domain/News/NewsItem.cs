namespace MyApp.Domain.News;

public class NewsItem
{
    public long Id { get; set; }
    public int SourceId { get; set; }
    public NewsSource Source { get; set; } = null!;
    public NewsTopic Topic { get; set; }
    public string ExternalId { get; set; } = "";   // guid записи или её ссылка
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Полный текст статьи, извлечённый со страницы источника (абзацы разделены пустой строкой). null: не удалось получить.</summary>
    public string? Content { get; set; }

    /// <summary>Когда страницу статьи обработали (успешно или нет): по этому полю новость больше не берётся в очередь.</summary>
    public DateTimeOffset? EnrichedAt { get; set; }

    /// <summary>Всегда UTC: Npgsql не принимает timestamptz с другим смещением.</summary>
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
