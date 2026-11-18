namespace MyApp.Domain.News;

public class NewsComment
{
    public long Id { get; set; }
    public long NewsItemId { get; set; }
    public Guid UserId { get; set; }               // тип подгоните под ваш ApplicationUser
    public string UserName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}