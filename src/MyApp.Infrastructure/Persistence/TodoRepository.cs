using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.Todos;
using MyApp.Domain.Todos;

namespace MyApp.Infrastructure.Persistence;

public class TodoRepository(AppDbContext db) : ITodoRepository
{
    public async Task<PagedResult<TodoItem>> ListAsync(string userId, PageRequest paging, CancellationToken ct)
    {
        var query = db.Todos.AsNoTracking().Where(t => t.UserId == userId);

        var total = await query.CountAsync(ct);

        // Id в конце сортировки делает порядок стабильным: задачи не «прыгают» между страницами
        var items = await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TodoItem>(items, paging.Page, paging.PageSize, total);
    }

    public Task<TodoItem?> FindAsync(int id, string userId, CancellationToken ct) =>
        db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, ct);

    public void Add(TodoItem todo) => db.Todos.Add(todo);

    public void Remove(TodoItem todo) => db.Todos.Remove(todo);

    public async Task<TodoStatsResponse> GetStatsAsync(string userId, DateOnly today, CancellationToken ct)
    {
        var stats = await db.Todos
            .Where(t => t.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new TodoStatsResponse(
                g.Count(),
                g.Count(t => t.IsDone),
                g.Count(t => !t.IsDone),
                g.Count(t => !t.IsDone && t.DueDate != null && t.DueDate < today),
                g.Count(t => !t.IsDone && t.Priority == Priority.High),
                g.Count(t => !t.IsDone && t.Priority == Priority.Normal),
                g.Count(t => !t.IsDone && t.Priority == Priority.Low)))
            .FirstOrDefaultAsync(ct);

        return stats ?? TodoStatsResponse.Empty;
    }
}
