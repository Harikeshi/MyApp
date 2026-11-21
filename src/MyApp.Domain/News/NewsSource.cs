namespace MyApp.Domain.News;

/// <summary>RSS/Atom-лента, которую опрашивает фоновый воркер.</summary>
public class NewsSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FeedUrl { get; set; } = "";
    public NewsTopic Topic { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Если не пусто, берём только новости, где есть хотя бы одно из слов (для общих лент).</summary>
    public string[] IncludeKeywords { get; set; } = [];

    public string? ETag { get; set; }
    public DateTimeOffset? LastFetchedAt { get; set; }
    public DateTimeOffset? LastSuccessAt { get; set; }
    public string? LastError { get; set; }
}
