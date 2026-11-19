// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
//
// namespace MyApp.Web.Controllers;
//
// [ApiController, Route("api/v1/news")]
// public class NewsController(INewsService news) : ControllerBase
// {
//     // Лента с keyset-пагинацией: ?before=<ISO-дата>&beforeId=123&sourceId=1&limit=20
//     [HttpGet]
//     public Task<NewsPageDto> List([FromQuery] NewsQuery q, CancellationToken ct) => news.GetPageAsync(q, ct);
//
//     [HttpGet("{id:long}/comments")]
//     public Task<List<CommentDto>> Comments(long id, CancellationToken ct) => news.GetCommentsAsync(id, ct);
//
//     [Authorize, HttpPost("{id:long}/comments")]
//     public async Task<ActionResult<CommentDto>> AddComment(long id, AddCommentRequest req, CancellationToken ct)
//         => Ok(await news.AddCommentAsync(id, req.Text, ct));
//
//     [Authorize, HttpDelete("comments/{commentId:long}")]   // только свой комментарий
//     public async Task<IActionResult> DeleteComment(long commentId, CancellationToken ct)
//         => await news.DeleteCommentAsync(commentId, ct) ? NoContent() : NotFound();
// }