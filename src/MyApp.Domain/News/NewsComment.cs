namespace MyApp.Domain.News;

public class NewsComment
{
    public long Id { get; set; }
    public long NewsItemId { get; set; }

    /// <summary>Id пользователя Identity (строка). Имя в ленте берётся из ApplicationUser.DisplayName.</summary>
    public string UserId { get; set; } = "";

    public string Text { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Скрыт по жалобам пользователей.</summary>
    public bool IsHidden { get; set; }
}
