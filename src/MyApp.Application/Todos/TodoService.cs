using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

public class TodoService(IAppDbContext db, ICurrentUser user, TimeProvider clock)
{
    public async Task<List<TodoResponse>> ListAsync(CancellationToken ct)
    {
        var uid = user.Id;

        return await db.Todos
            .Where(t => t.UserId == uid)
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt, t.DueDate, t.Priority))
            .ToListAsync(ct);
    }

    public async Task<TodoResponse> CreateAsync(
        string title, DateOnly? dueDate, Priority priority, CancellationToken ct)
    {
        var todo = new TodoItem(user.Id, title, dueDate, priority, clock.GetUtcNow().UtcDateTime);

        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);

        return ToResponse(todo);
    }

    public async Task<bool> UpdateAsync(
        int id, string title, bool isDone, DateOnly? dueDate, Priority priority, CancellationToken ct)
    {
        var uid = user.Id;
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid, ct);
        if (todo is null) return false;

        todo.Update(title, isDone, dueDate, priority);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var uid = user.Id;
        var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid, ct);
        if (todo is null) return false;

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TodoResponse ToResponse(TodoItem t) =>
        new(t.Id, t.Title, t.IsDone, t.CreatedAt, t.DueDate, t.Priority);
    
    // todo: Для теста вкладки

    public async Task<TodoStatsResponse> GetStatsAsync(CancellationToken ct)
    {
        var uid = user.Id;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        return await db.Todos
            .Where(t => t.UserId == uid)
            .GroupBy(_ => 1)
            .Select(g => new TodoStatsResponse(
                g.Count(),
                g.Count(t => t.IsDone),
                g.Count(t => !t.IsDone),
                g.Count(t => !t.IsDone && t.DueDate != null && t.DueDate < today),
                g.Count(t => !t.IsDone && t.Priority == Priority.High)))
            .FirstOrDefaultAsync(ct) ?? new TodoStatsResponse(0, 0, 0, 0, 0);
    }
}
public record TodoStatsResponse(int Total, int Done, int Open, int Overdue, int HighPriorityOpen);
