using MyApp.Application.News;
using MyApp.Domain.News;
using MyApp.Web.Models;

namespace MyApp.Web.Endpoints;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNews(this IEndpointRouteBuilder app)
    {
        // Адреса вне /api: /api/** целиком уходит в AiChat через YARP.
        var news = app.MapGroup("/news").RequireAuthorization();

        // GET /news?topic=crypto|markets|manuls&cursor=...&limit=20 -> { items, nextCursor }
        // Без topic приходят все темы; nextCursor передаётся в следующий запрос (null: новостей больше нет).
        // topic приходит строкой и разбирается без учёта регистра: встроенный биндинг enum не принимает "markets".
        news.MapGet("/", async (string? topic, string? cursor, int? limit, NewsService service, CancellationToken ct) =>
        {
            NewsTopic? parsed = null;
            if (!string.IsNullOrWhiteSpace(topic))
            {
                // IsDefined отсекает числа ("7") и перечисления через запятую, которые Enum.TryParse пропускает.
                if (!Enum.TryParse<NewsTopic>(topic, ignoreCase: true, out var t) || !Enum.IsDefined(t))
                    return Results.Problem(
                        title: "Неизвестная тема", detail: "Допустимые темы: markets, crypto, manuls.",
                        statusCode: StatusCodes.Status400BadRequest);
                parsed = t;
            }

            return Results.Ok(await service.ListAsync(parsed, cursor, limit, ct));
        });

        // Полный текст статьи для «Читать полностью»: в список он не входит (до 20 тысяч символов на новость).
        news.MapGet("/{id:long}/content", async (long id, NewsService service, CancellationToken ct) =>
            await service.GetContentAsync(id, ct) is { } content
                ? Results.Ok(content)
                : Results.NotFound());

        news.MapGet("/{id:long}/comments", async (long id, NewsService service, CancellationToken ct) =>
            await service.ListCommentsAsync(id, ct) is { } comments
                ? Results.Ok(comments)
                : Results.NotFound());

        news.MapPost("/{id:long}/comments", async (long id, AddCommentRequest req, NewsService service, CancellationToken ct) =>
            {
                var result = await service.AddCommentAsync(id, req.Text, ct);
                return result.Status switch
                {
                    AddCommentStatus.Created => Results.Created($"/news/{id}/comments", result.Comment),
                    AddCommentStatus.NotFound => Results.NotFound(),
                    _ => Results.Problem(
                        title: "Комментарий отклонён", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
                };
            })
            .RequireRateLimiting("news-comments");

        // Перевод по кнопке: первый запрос переводит и сохраняет, дальше перевод берётся из базы.
        news.MapPost("/{id:long}/translation", async (long id, NewsService service, CancellationToken ct) =>
            {
                var result = await service.TranslateAsync(id, ct);
                return result.Status switch
                {
                    TranslationStatus.Ok => Results.Ok(result.Translation),
                    TranslationStatus.NotFound => Results.NotFound(),
                    TranslationStatus.LimitReached => Results.Problem(
                        detail: "Суточный лимит переводов исчерпан, попробуйте завтра.",
                        statusCode: StatusCodes.Status429TooManyRequests),
                    _ => Results.Problem(
                        detail: "Перевод временно недоступен.",
                        statusCode: StatusCodes.Status503ServiceUnavailable)
                };
            })
            .RequireRateLimiting("news-translate");

        // Удалить можно только свой комментарий.
        news.MapDelete("/comments/{commentId:long}", async (long commentId, NewsService service, CancellationToken ct) =>
            await service.DeleteCommentAsync(commentId, ct) ? Results.NoContent() : Results.NotFound());

        news.MapPost("/comments/{commentId:long}/report", async (long commentId, NewsService service, CancellationToken ct) =>
                await service.ReportCommentAsync(commentId, ct) switch
                {
                    ReportOutcome.Reported => Results.NoContent(),
                    ReportOutcome.AlreadyReported => Results.Problem(
                        detail: "Вы уже жаловались на этот комментарий.", statusCode: StatusCodes.Status409Conflict),
                    ReportOutcome.OwnComment => Results.Problem(
                        detail: "Нельзя пожаловаться на свой комментарий.", statusCode: StatusCodes.Status400BadRequest),
                    _ => Results.NotFound()
                })
            .RequireRateLimiting("news-comments");

        return app;
    }
}
