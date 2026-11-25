using System.ComponentModel.DataAnnotations;
using MyApp.Domain.Todos;            // Priority, TodoItem.MaxTitleLength

namespace MyApp.Web.Models;

// Модели ЗАПРОСОВ остаются в Web: проверка Minimal API находит их рядом с эндпоинтами.
// Модели ответов (TodoResponse, ProjectResponse, ...) живут в MyApp.Application.

public record CreateTodoRequest
{
    [Required, StringLength(TodoItem.MaxTitleLength, MinimumLength = 1)]
    public string Title { get; init; } = "";

    /// <summary>Срок (дата и время, ISO 8601). Не указан — текущее время.</summary>
    public DateTime? DueAt { get; init; }

    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Normal;

    /// <summary>Исполнитель: сам пользователь или участник проекта; не указан — без исполнителя.</summary>
    public string? AssigneeId { get; init; }

    /// <summary>Проект; не указан — задача без проекта.</summary>
    public int? ProjectId { get; init; }
}

public record UpdateTodoRequest
{
    [Required, StringLength(TodoItem.MaxTitleLength, MinimumLength = 1)]
    public string Title { get; init; } = "";

    [Required]
    public DateTime? DueAt { get; init; }

    [EnumDataType(typeof(Priority))]
    public Priority Priority { get; init; } = Priority.Normal;

    public string? AssigneeId { get; init; }
}

public record SetDoneRequest
{
    public bool IsDone { get; init; }
}

public record ProjectRequest
{
    [Required, StringLength(Project.MaxNameLength, MinimumLength = 1)]
    public string Name { get; init; } = "";

    /// <summary>Id ответственных (владелец входит в проект сам).</summary>
    public List<string>? MemberIds { get; init; }
}
