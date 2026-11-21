using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Application.News;
using MyApp.Domain.News;
using Npgsql;

namespace MyApp.Infrastructure.Persistence;

public class NewsRepository(AppDbContext db) : INewsRepository
{
    private const string UnknownUserName = "Пользователь";

    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public async Task<NewsPageResponse> ListAsync(NewsTopic? topic, NewsCursor? cursor, int limit, string lang, CancellationToken ct)
    {
        IQueryable<NewsItem> query = db.News.AsNoTracking();

        if (topic is { } t) query = query.Where(n => n.Topic == t);
        if (cursor is { } c)
            query = query.Where(n => n.PublishedAt < c.PublishedAt || (n.PublishedAt == c.PublishedAt && n.Id < c.Id));

        // Id в конце сортировки делает порядок стабильным; лишняя запись означает «есть следующая страница».
        var rows = await query
            .OrderByDescending(n => n.PublishedAt).ThenByDescending(n => n.Id)
            .Take(limit + 1)
            .Select(n => new
            {
                n.Id, SourceName = n.Source.Name, n.Topic, n.Title, n.Url, n.Summary, n.ImageUrl, n.PublishedAt
            })
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        var ids = rows.Select(r => r.Id).ToList();
        var counts = await db.NewsComments.AsNoTracking()
            .Where(x => ids.Contains(x.NewsItemId) && !x.IsHidden)
            .GroupBy(x => x.NewsItemId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var translations = await db.NewsTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.NewsItemId) && t.Lang == lang)
            .ToDictionaryAsync(t => t.NewsItemId, t => new TranslationResponse(t.Title, t.Summary), ct);

        var items = rows.Select(r => new NewsItemResponse(
            r.Id, r.SourceName, r.Topic, r.Title, r.Url, r.Summary, r.ImageUrl, r.PublishedAt,
            counts.GetValueOrDefault(r.Id),
            translations.GetValueOrDefault(r.Id))).ToList();

        var next = hasMore && rows.Count > 0
            ? new NewsCursor(rows[^1].PublishedAt, rows[^1].Id).Encode()
            : null;

        return new NewsPageResponse(items, next);
    }

    public Task<bool> ExistsAsync(long newsId, CancellationToken ct) =>
        db.News.AnyAsync(n => n.Id == newsId, ct);

    public Task<NewsItem?> FindAsync(long newsId, CancellationToken ct) =>
        db.News.AsNoTracking().FirstOrDefaultAsync(n => n.Id == newsId, ct);

    public Task<TranslationResponse?> GetTranslationAsync(long newsId, string lang, CancellationToken ct) =>
        db.NewsTranslations.AsNoTracking()
            .Where(t => t.NewsItemId == newsId && t.Lang == lang)
            .Select(t => new TranslationResponse(t.Title, t.Summary))
            .FirstOrDefaultAsync(ct);

    public Task<int> CountTranslationsSinceAsync(DateTimeOffset since, CancellationToken ct) =>
        db.NewsTranslations.CountAsync(t => t.CreatedAt >= since, ct);

    public async Task<TranslationResponse> SaveTranslationAsync(NewsTranslation translation, CancellationToken ct)
    {
        db.NewsTranslations.Add(translation);
        try
        {
            await db.SaveChangesAsync(ct);
            return new TranslationResponse(translation.Title, translation.Summary);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Другой пользователь успел перевести эту же новость: отдаём то, что уже лежит в базе.
            db.Entry(translation).State = EntityState.Detached;
            return (await GetTranslationAsync(translation.NewsItemId, translation.Lang, ct))!;
        }
    }

    public async Task<IReadOnlyList<CommentResponse>> ListCommentsAsync(long newsId, string currentUserId, CancellationToken ct)
    {
        // Имя берём из профиля: смена имени в «Настройках» сразу видна в старых комментариях.
        var rows = await (
                from c in db.NewsComments.AsNoTracking()
                where c.NewsItemId == newsId && !c.IsHidden
                join u in db.Users on c.UserId equals u.Id
                orderby c.CreatedAt, c.Id
                select new { c.Id, u.DisplayName, c.Text, c.CreatedAt, c.UserId })
            .Take(500)
            .ToListAsync(ct);

        return rows.Select(r => new CommentResponse(
            r.Id, NameOrDefault(r.DisplayName), r.Text, r.CreatedAt, r.UserId == currentUserId)).ToList();
    }

    public async Task<CommentResponse?> GetCommentAsync(long commentId, string currentUserId, CancellationToken ct)
    {
        var row = await (
                from c in db.NewsComments.AsNoTracking()
                where c.Id == commentId
                join u in db.Users on c.UserId equals u.Id
                select new { c.Id, u.DisplayName, c.Text, c.CreatedAt, c.UserId })
            .FirstOrDefaultAsync(ct);

        return row is null
            ? null
            : new CommentResponse(row.Id, NameOrDefault(row.DisplayName), row.Text, row.CreatedAt, row.UserId == currentUserId);
    }

    public void AddComment(NewsComment comment) => db.NewsComments.Add(comment);

    public async Task<bool> DeleteCommentAsync(long commentId, string userId, CancellationToken ct) =>
        await db.NewsComments.Where(c => c.Id == commentId && c.UserId == userId).ExecuteDeleteAsync(ct) > 0;

    public async Task<ReportOutcome> ReportCommentAsync(long commentId, string userId, int hideAfter, CancellationToken ct)
    {
        var author = await db.NewsComments.AsNoTracking()
            .Where(c => c.Id == commentId && !c.IsHidden)
            .Select(c => c.UserId)
            .FirstOrDefaultAsync(ct);

        if (author is null) return ReportOutcome.NotFound;
        if (author == userId) return ReportOutcome.OwnComment;

        db.NewsCommentReports.Add(new NewsCommentReport
        {
            CommentId = commentId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return ReportOutcome.AlreadyReported;
        }

        var reports = await db.NewsCommentReports.CountAsync(r => r.CommentId == commentId, ct);
        if (reports >= hideAfter)
        {
            await db.NewsComments.Where(c => c.Id == commentId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsHidden, true), ct);
        }

        return ReportOutcome.Reported;
    }

    private static string NameOrDefault(string? name) =>
        string.IsNullOrWhiteSpace(name) ? UnknownUserName : name;
}
