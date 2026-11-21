// using System.Security.Claims;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.RateLimiting;
// using MyApp.Application.News;
//
// namespace MyApp.Web.Controllers;
//
// [ApiController]
// [Route("api/v1/news")]
// public sealed class NewsController(INewsService news) : ControllerBase
// {
//     // Если у вас есть ICurrentUser, возьмите id и имя оттуда и удалите эти два свойства.
//     private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
//
//     private string UserName
//     {
//         get
//         {
//             var name = User.FindFirstValue("display_name") ?? User.FindFirstValue(ClaimTypes.Name);
//             // Не показываем email, если Identity-имя это почта.
//             return string.IsNullOrWhiteSpace(name) || name.Contains('@') ? "Пользователь" : name.Trim();
//         }
//     }
//
//     /// <summary>Лента. ?topic=Crypto|Markets&amp;cursor=...&amp;limit=20</summary>
//     [HttpGet]
//     public Task<NewsPageDto> List([FromQuery] NewsQuery query, CancellationToken ct)
//         => news.GetPageAsync(query, ct);
//
//     [HttpGet("{id:long}/comments")]
//     public async Task<ActionResult<IReadOnlyList<CommentDto>>> Comments(long id, CancellationToken ct)
//         => await news.GetCommentsAsync(id, UserId, ct) is { } list ? Ok(list) : NotFound();
//
//     [Authorize]
//     [EnableRateLimiting("news-comments")]
//     [HttpPost("{id:long}/comments")]
//     public async Task<ActionResult<CommentDto>> AddComment(long id, AddCommentRequest request, CancellationToken ct)
//     {
//         var result = await news.AddCommentAsync(id, UserId!, UserName, request.Text, ct);
//         return result.Status switch
//         {
//             AddCommentStatus.Ok => Ok(result.Comment),
//             AddCommentStatus.NotFound => NotFound(),
//             _ => Problem(detail: result.Error, statusCode: StatusCodes.Status400BadRequest, title: "Комментарий отклонён")
//         };
//     }
//
//     [Authorize]
//     [HttpDelete("comments/{commentId:long}")]
//     public async Task<IActionResult> DeleteComment(long commentId, CancellationToken ct)
//         => await news.DeleteCommentAsync(commentId, UserId!, ct) ? NoContent() : NotFound();
//
//     [Authorize]
//     [EnableRateLimiting("news-comments")]
//     [HttpPost("comments/{commentId:long}/report")]
//     public async Task<IActionResult> ReportComment(long commentId, CancellationToken ct)
//         => await news.ReportCommentAsync(commentId, UserId!, ct) switch
//         {
//             ReportOutcome.Reported => NoContent(),
//             ReportOutcome.AlreadyReported => Problem(detail: "Вы уже жаловались на этот комментарий.", statusCode: StatusCodes.Status409Conflict),
//             ReportOutcome.OwnComment => Problem(detail: "Нельзя пожаловаться на свой комментарий.", statusCode: StatusCodes.Status400BadRequest),
//             _ => NotFound()
//         };
//
//     /// <summary>Запасной вариант, если фоновый воркер ещё не успел перевести новость.</summary>
//     [Authorize]
//     [EnableRateLimiting("news-translate")]
//     [HttpPost("{id:long}/translation")]
//     public async Task<ActionResult<TranslationDto>> Translate(long id, CancellationToken ct)
//     {
//         var result = await news.GetOrCreateTranslationAsync(id, ct);
//         return result.Status switch
//         {
//             TranslationStatus.Ok => Ok(result.Translation),
//             TranslationStatus.NotFound => NotFound(),
//             _ => Problem(detail: "Перевод временно недоступен.", statusCode: StatusCodes.Status503ServiceUnavailable)
//         };
//     }
// }
