using System.ComponentModel.DataAnnotations;
using MyApp.Domain.Todos;            // для Priority

namespace MyApp.Web.Models;

// Модели ЗАПРОСОВ остаются в Web: проверка Minimal API находит их рядом с эндпоинтами.
// Модель ответа (TodoResponse) теперь живёт в MyApp.Application.Todos.

public record CreateTodoRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = "";
    public DateOnly? DueDate { get; init; }
    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Normal;
}

public record UpdateTodoRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = "";
    public bool IsDone { get; init; }
    public DateOnly? DueDate { get; init; }
    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Normal;
}