using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.Todos;
using MyApp.Domain.Todos;

namespace MyApp.Infrastructure.Persistence;

public class TodoRepository(AppDbContext db) : ITodoRepository
{
    public async Task<PagedResult<TodoEntry>> ListAsync(
        string userId, int? projectId, PageRequest paging, CancellationToken ct)
    {
        var query = db.Todos.AsNoTracking().VisibleTo(db, userId);
        if (projectId is int pid)
            query = query.Where(t => t.ProjectId == pid);

        var total = await query.CountAsync(ct);

        // Id в конце сортировки делает порядок стабильным: задачи не «прыгают» между страницами
        var items = await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueAt)
            .ThenBy(t => t.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(ct);

        var projectIds = items.Where(t => t.ProjectId != null).Select(t => t.ProjectId!.Value).Distinct().ToList();
        var owners = projectIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Projects.AsNoTracking()
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.OwnerId, ct);

        var entries = items
            .Select(t => new TodoEntry(t, t.ProjectId is int id && owners.TryGetValue(id, out var owner) ? owner : null))
            .ToList();
        return new PagedResult<TodoEntry>(entries, paging.Page, paging.PageSize, total);
    }

    public async Task<TodoEntry?> FindAsync(int id, string userId, CancellationToken ct)
    {
        var todo = await db.Todos.VisibleTo(db, userId).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (todo is null) return null;

        string? ownerId = null;
        if (todo.ProjectId is int pid)
            ownerId = await db.Projects.Where(p => p.Id == pid).Select(p => p.OwnerId).FirstOrDefaultAsync(ct);

        return new TodoEntry(todo, ownerId);
    }

    public void Add(TodoItem todo) => db.Todos.Add(todo);

    public void Remove(TodoItem todo) => db.Todos.Remove(todo);

    public async Task<TodoStatsResponse> GetStatsAsync(string userId, DateTime todayStartUtc, CancellationToken ct)
    {
        var stats = await db.Todos
            .VisibleTo(db, userId)
            .GroupBy(_ => 1)
            .Select(g => new TodoStatsResponse(
                g.Count(),
                g.Count(t => t.IsDone),
                g.Count(t => !t.IsDone),
                g.Count(t => !t.IsDone && t.DueAt < todayStartUtc),
                g.Count(t => !t.IsDone && t.Priority == Priority.High),
                g.Count(t => !t.IsDone && t.Priority == Priority.Normal),
                g.Count(t => !t.IsDone && t.Priority == Priority.Low)))
            .FirstOrDefaultAsync(ct);

        return stats ?? TodoStatsResponse.Empty;
    }
}
