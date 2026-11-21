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

        // GET /news?topic=crypto|markets&cursor=...&limit=20 -> { items, nextCursor }
        /// topic приходит строкой и разбирается без учёта регистра: встроенный биндинг enum не принимает "markets".
        news.MapGet("/", async (string? topic, string? cursor, int? limit, NewsService service, CancellationToken ct) =>
        {
            NewsTopic? parsed = null;
            if (!string.IsNullOrWhiteSpace(topic))
            {
                // IsDefined отсекает числа ("7") и перечисления через запятую, которые Enum.TryParse пропускает.
                if (!Enum.TryParse<NewsTopic>(topic, ignoreCase: true, out var t) || !Enum.IsDefined(t))
                    return Results.Problem(
                        title: "Неизвестная тема", detail: "Допустимые темы: markets, crypto.",
                        statusCode: StatusCodes.Status400BadRequest);
                parsed = t;
            }

            return Results.Ok(await service.ListAsync(parsed, cursor, limit, ct));
        });

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
