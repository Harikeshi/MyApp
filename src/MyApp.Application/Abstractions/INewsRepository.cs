using MyApp.Application.News;
using MyApp.Domain.News;

namespace MyApp.Application.Abstractions;

/// <summary>Порт хранилища новостей и комментариев. Реализация (EF Core + PostgreSQL) лежит в Infrastructure.</summary>
public interface INewsRepository
{
    /// <summary>
    /// Страница ленты, новые сверху. <paramref name="cursor"/> null: с самого начала.
    /// К новостям, которые уже переводили на <paramref name="lang"/>, прикладывается перевод.
    /// </summary>
    Task<NewsPageResponse> ListAsync(NewsTopic? topic, NewsCursor? cursor, int limit, string lang, CancellationToken ct);

    Task<bool> ExistsAsync(long newsId, CancellationToken ct);

    Task<NewsItem?> FindAsync(long newsId, CancellationToken ct);

    Task<TranslationResponse?> GetTranslationAsync(long newsId, string lang, CancellationToken ct);

    /// <summary>Сколько переводов создано начиная с <paramref name="since"/>.</summary>
    Task<int> CountTranslationsSinceAsync(DateTimeOffset since, CancellationToken ct);

    /// <summary>Сохраняет перевод сразу (без IUnitOfWork); если параллельно уже сохранили, возвращает существующий.</summary>
    Task<TranslationResponse> SaveTranslationAsync(NewsTranslation translation, CancellationToken ct);

    /// <summary>Видимые комментарии к новости, старые сверху. Имя автора берётся из его профиля.</summary>
    Task<IReadOnlyList<CommentResponse>> ListCommentsAsync(long newsId, string currentUserId, CancellationToken ct);

    Task<CommentResponse?> GetCommentAsync(long commentId, string currentUserId, CancellationToken ct);

    void AddComment(NewsComment comment);

    /// <summary>Удаляет комментарий, только если он принадлежит пользователю.</summary>
    Task<bool> DeleteCommentAsync(long commentId, string userId, CancellationToken ct);

    /// <summary>Записывает жалобу (сразу, без IUnitOfWork); после <paramref name="hideAfter"/> разных жалоб комментарий скрывается.</summary>
    Task<ReportOutcome> ReportCommentAsync(long commentId, string userId, int hideAfter, CancellationToken ct);
}
