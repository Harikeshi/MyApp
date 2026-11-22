using MyApp.Application.Abstractions;
using MyApp.Domain.News;

namespace MyApp.Application.News;

public class NewsService(
    INewsRepository news, INewsTranslator translator, IUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    /// <summary>Столько разных жалоб скрывают комментарий.</summary>
    private const int ReportsToHide = 3;

    /// <param name="topic">null: все темы.</param>
    /// <param name="cursor">NextCursor из предыдущего ответа; неверное значение игнорируется (лента с начала).</param>
    /// <param name="limit">По умолчанию 20, не больше 50.</param>
    public Task<NewsPageResponse> ListAsync(NewsTopic? topic, string? cursor, int? limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        NewsCursor? parsed = NewsCursor.TryParse(cursor, out var c) ? c : null;
        return news.ListAsync(topic, parsed, take, translator.TargetLang, ct);
    }

    /// <returns>null: такой новости нет.</returns>
    public async Task<IReadOnlyList<CommentResponse>?> ListCommentsAsync(long newsId, CancellationToken ct) =>
        await news.ExistsAsync(newsId, ct)
            ? await news.ListCommentsAsync(newsId, user.Id, ct)
            : null;

    /// <returns>null: такой новости нет.</returns>
    public async Task<NewsContentResponse?> GetContentAsync(long newsId, CancellationToken ct) =>
        await news.ExistsAsync(newsId, ct)
            ? new NewsContentResponse(await news.GetContentAsync(newsId, ct))
            : null;

    public async Task<AddCommentResult> AddCommentAsync(long newsId, string? text, CancellationToken ct)
    {
        if (!CommentRules.TryNormalize(text, out var clean, out var error))
            return new(AddCommentStatus.Invalid, Error: error);

        if (!await news.ExistsAsync(newsId, ct))
            return new(AddCommentStatus.NotFound);

        var comment = new NewsComment
        {
            NewsItemId = newsId,
            UserId = user.Id,
            Text = clean,
            CreatedAt = clock.GetUtcNow()
        };
        news.AddComment(comment);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await news.GetCommentAsync(comment.Id, user.Id, ct);
        return new(AddCommentStatus.Created, created);
    }

    public Task<bool> DeleteCommentAsync(long commentId, CancellationToken ct) =>
        news.DeleteCommentAsync(commentId, user.Id, ct);

    public Task<ReportOutcome> ReportCommentAsync(long commentId, CancellationToken ct) =>
        news.ReportCommentAsync(commentId, user.Id, ReportsToHide, ct);

    /// <summary>
    /// Перевод по кнопке. Готовый перевод берётся из базы (он общий для всех пользователей),
    /// новый создаётся только если не исчерпан суточный лимит.
    /// </summary>
    public async Task<TranslationResult> TranslateAsync(long newsId, CancellationToken ct)
    {
        var item = await news.FindAsync(newsId, ct);
        if (item is null) return new(TranslationStatus.NotFound);

        var lang = translator.TargetLang;

        // Новость уже на русском (например, из русскоязычной ленты): переводить нечего и платить незачем.
        if (lang == "ru" && TextLanguage.IsMostlyCyrillic(item.Title))
            return new(TranslationStatus.Ok, new TranslationResponse(item.Title, item.Summary));

        var cached = await news.GetTranslationAsync(newsId, lang, ct);
        if (cached is not null) return new(TranslationStatus.Ok, cached);

        if (!translator.IsConfigured) return new(TranslationStatus.Unavailable);

        var since = clock.GetUtcNow().AddDays(-1);
        if (await news.CountTranslationsSinceAsync(since, ct) >= translator.DailyLimit)
            return new(TranslationStatus.LimitReached);

        try
        {
            var (title, summary) = await translator.TranslateAsync(item.Title, item.Summary, ct);
            var saved = await news.SaveTranslationAsync(new NewsTranslation
            {
                NewsItemId = newsId,
                Lang = lang,
                Title = title,
                Summary = summary,
                Model = translator.Model,
                CreatedAt = clock.GetUtcNow()
            }, ct);
            return new(TranslationStatus.Ok, saved);
        }
        catch (TranslationFailedException)
        {
            return new(TranslationStatus.Unavailable);
        }
    }
}
