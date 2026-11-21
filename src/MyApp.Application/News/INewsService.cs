// namespace MyApp.Application.News;
//
// public interface INewsService
// {
//     Task<NewsPageDto> GetPageAsync(NewsQuery q, CancellationToken ct);
//
//     /// <summary>null — новости не существует.</summary>
//     Task<IReadOnlyList<CommentDto>?> GetCommentsAsync(long itemId, string? currentUserId, CancellationToken ct);
//     Task<AddCommentResult> AddCommentAsync(long itemId, string userId, string userName, string? text, CancellationToken ct);
//     Task<bool> DeleteCommentAsync(long commentId, string userId, CancellationToken ct);
//     Task<ReportOutcome> ReportCommentAsync(long commentId, string userId, CancellationToken ct);
//
//     Task<TranslationResult> GetOrCreateTranslationAsync(long itemId, CancellationToken ct);
//
//     /// <summary>Вызовите при смене имени пользователя (раздел «Настройки»).</summary>
//     Task OnUserRenamedAsync(string userId, string newName, CancellationToken ct);
//
//     /// <summary>Вызовите при удалении аккаунта.</summary>
//     Task OnUserDeletedAsync(string userId, CancellationToken ct);
// }
