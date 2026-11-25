using MyApp.Application.Common;
using MyApp.Application.Todos;
using MyApp.Domain.Todos;

namespace MyApp.Application.Abstractions;

/// <summary>Задача и владелец её проекта (null, если задача без проекта): по ним решается, кто может её менять.</summary>
public record TodoEntry(TodoItem Todo, string? ProjectOwnerId);

/// <summary>
/// Порт хранилища задач. Пользователь видит задачу, если он её автор, исполнитель
/// или владелец/ответственный проекта, в который она входит.
/// </summary>
public interface ITodoRepository
{
    /// <summary>Страница видимых задач (все или одного проекта): незавершённые, по приоритету, по сроку.</summary>
    Task<PagedResult<TodoEntry>> ListAsync(string userId, int? projectId, PageRequest paging, CancellationToken ct);

    /// <summary>Задача по id (с отслеживанием), только если она видна пользователю.</summary>
    Task<TodoEntry?> FindAsync(int id, string userId, CancellationToken ct);

    void Add(TodoItem todo);

    void Remove(TodoItem todo);

    /// <param name="todayStartUtc">Начало текущего дня клиента: всё, что раньше, считается просроченным.</param>
    Task<TodoStatsResponse> GetStatsAsync(string userId, DateTime todayStartUtc, CancellationToken ct);
}
