namespace MyApp.Domain.News;

public class NewsSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FeedUrl { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string? ETag { get; set; }
    public DateTimeOffset? LastFetchedAt { get; set; }
    public string? LastError { get; set; }
}