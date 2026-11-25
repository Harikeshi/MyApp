using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

/// <param name="CanManage">Может ли текущий пользователь менять и удалять задачу (автор или владелец проекта).</param>
public record TodoResponse(
    int Id,
    string Title,
    bool IsDone,
    DateTime CreatedAt,
    DateTime DueAt,
    Priority Priority,
    int? ProjectId,
    string OwnerId,
    string OwnerName,
    string? AssigneeId,
    string? AssigneeName,
    bool CanManage);
