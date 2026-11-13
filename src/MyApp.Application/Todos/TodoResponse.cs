using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

public record TodoResponse(
    int Id,
    string Title,
    bool IsDone,
    DateTime CreatedAt,
    DateOnly? DueDate,
    Priority Priority);
