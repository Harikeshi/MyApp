using MyApp.Application.Common;
using MyApp.Application.Todos;
using MyApp.Domain.Todos;

namespace MyApp.Application.Abstractions;

/// <summary>Порт хранилища задач. Реализация (EF Core + PostgreSQL) лежит в Infrastructure.</summary>
public interface ITodoRepository
{
    /// <summary>Страница задач пользователя в порядке показа: незавершённые, по приоритету, по сроку.</summary>
    Task<PagedResult<TodoItem>> ListAsync(string userId, PageRequest paging, CancellationToken ct);

    /// <summary>Задача по id, только если она принадлежит пользователю.</summary>
    Task<TodoItem?> FindAsync(int id, string userId, CancellationToken ct);

    void Add(TodoItem todo);

    void Remove(TodoItem todo);

    /// <param name="today">День, относительно которого считаются просроченные.</param>
    Task<TodoStatsResponse> GetStatsAsync(string userId, DateOnly today, CancellationToken ct);
}
