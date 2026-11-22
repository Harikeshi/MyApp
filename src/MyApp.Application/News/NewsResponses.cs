using MyApp.Domain.News;

namespace MyApp.Application.News;

public record TranslationResponse(string Title, string? Summary);

public record NewsItemResponse(
    long Id,
    string SourceName,
    NewsTopic Topic,
    string Title,
    string Url,
    string? Summary,
    string? ImageUrl,
    bool HasContent,                     // true: у новости есть полный текст (GET /news/{id}/content)
    DateTimeOffset PublishedAt,
    int CommentCount,
    TranslationResponse? Translation);   // null: перевод ещё не делали

/// <param name="Content">Абзацы через пустую строку; null: полного текста нет.</param>
public record NewsContentResponse(string? Content);

/// <param name="NextCursor">null: это последняя страница.</param>
public record NewsPageResponse(IReadOnlyList<NewsItemResponse> Items, string? NextCursor);

public record CommentResponse(long Id, string UserName, string Text, DateTimeOffset CreatedAt, bool IsMine);

public enum AddCommentStatus { Created, NotFound, Invalid }

public record AddCommentResult(AddCommentStatus Status, CommentResponse? Comment = null, string? Error = null);

public enum ReportOutcome { Reported, AlreadyReported, OwnComment, NotFound }

public enum TranslationStatus { Ok, NotFound, Unavailable, LimitReached }

public record TranslationResult(TranslationStatus Status, TranslationResponse? Translation = null);

/// <summary>Сервис перевода недоступен или вернул непригодный ответ. Подробности в журнале Infrastructure.</summary>
public sealed class TranslationFailedException(string message, Exception? inner = null) : Exception(message, inner);
