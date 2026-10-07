using System.ComponentModel.DataAnnotations;

namespace MyApp.Web.Models;

public record CreateTodoRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = "";
}

public record UpdateTodoRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = "";
    public bool IsDone { get; init; }
}

public record TodoResponse(int Id, string Title, bool IsDone, DateTime CreatedAt);