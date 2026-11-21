namespace MyApp.Domain.News;

/// <summary>Жалоба пользователя на комментарий. Одна жалоба от пользователя на комментарий.</summary>
public class NewsCommentReport
{
    public long Id { get; set; }
    public long CommentId { get; set; }
    public string UserId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
