namespace MyApp.Domain.News;

public class NewsTranslation
{
    public long Id { get; set; }
    public long NewsItemId { get; set; }
    public string Lang { get; set; } = "ru";
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public string Model { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}