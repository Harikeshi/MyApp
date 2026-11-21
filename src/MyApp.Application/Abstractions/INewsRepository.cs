using MyApp.Application.News;
using MyApp.Domain.News;

namespace MyApp.Application.Abstractions;

/// <summary>Порт хранилища новостей и комментариев. Реализация (EF Core + PostgreSQL) лежит в Infrastructure.</summary>
public interface INewsRepository
{
    /// <summary>Страница ленты, новые сверху. <paramref name="cursor"/> null: с самого начала.</summary>
    Task<NewsPageResponse> ListAsync(NewsTopic? topic, NewsCursor? cursor, int limit, CancellationToken ct);

    Task<bool> ExistsAsync(long newsId, CancellationToken ct);

    /// <summary>Видимые комментарии к новости, старые сверху. Имя автора берётся из его профиля.</summary>
    Task<IReadOnlyList<CommentResponse>> ListCommentsAsync(long newsId, string currentUserId, CancellationToken ct);

    Task<CommentResponse?> GetCommentAsync(long commentId, string currentUserId, CancellationToken ct);

    void AddComment(NewsComment comment);

    /// <summary>Удаляет комментарий, только если он принадлежит пользователю.</summary>
    Task<bool> DeleteCommentAsync(long commentId, string userId, CancellationToken ct);

    /// <summary>Записывает жалобу (сразу, без IUnitOfWork); после <paramref name="hideAfter"/> разных жалоб комментарий скрывается.</summary>
    Task<ReportOutcome> ReportCommentAsync(long commentId, string userId, int hideAfter, CancellationToken ct);
}
