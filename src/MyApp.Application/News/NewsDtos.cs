using MyApp.Domain.News;

namespace MyApp.Application.News;

public record NewsQuery(NewsTopic? Topic = null, int? SourceId = null, string? Cursor = null, int Limit = 20);

public record TranslationDto(string Title, string? Summary);

public record NewsItemDto(
    long Id,
    string SourceName,
    NewsTopic Topic,
    string Title,
    string Url,
    string? Summary,
    string? ImageUrl,
    DateTimeOffset PublishedAt,
    int CommentCount,
    TranslationDto? Translation);

public record NewsPageDto(IReadOnlyList<NewsItemDto> Items, string? NextCursor);

public record CommentDto(long Id, string UserName, string Text, DateTimeOffset CreatedAt, bool IsMine);

public record AddCommentRequest(string? Text);

// public enum AddCommentStatus { Ok, NotFound, Invalid }
// public record AddCommentResult(AddCommentStatus Status, CommentDto? Comment = null, string? Error = null);

public enum TranslationStatus { Ok, NotFound, Unavailable }
public record TranslationResult(TranslationStatus Status, TranslationDto? Translation = null);

// public enum ReportOutcome { Reported, AlreadyReported, OwnComment, NotFound }
