using System.ComponentModel.DataAnnotations;
using MyApp.Application.News;

namespace MyApp.Web.Models;

// Модели ЗАПРОСОВ остаются в Web (как и для задач). Ответы лежат в MyApp.Application.News.

public record AddCommentRequest
{
    [Required, StringLength(CommentRules.MaxLength, MinimumLength = 1)]
    public string Text { get; init; } = "";
}
