using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

public class TodoService(ITodoRepository todos, IUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
{
    /// <param name="page">Номер страницы с 1 (по умолчанию 1).</param>
    /// <param name="pageSize">Размер страницы (по умолчанию 20, не больше 100); неверные значения приводятся к допустимым.</param>
    public async Task<PagedResult<TodoResponse>> ListAsync(int? page, int? pageSize, CancellationToken ct)
    {
        var result = await todos.ListAsync(user.Id, PageRequest.Of(page, pageSize), ct);
        return result.Map(ToResponse);
    }

    public async Task<TodoResponse> CreateAsync(
        string title, DateOnly? dueDate, Priority priority, CancellationToken ct)
    {
        var todo = new TodoItem(user.Id, title, dueDate, priority, clock.GetUtcNow().UtcDateTime);

        todos.Add(todo);
        await unitOfWork.SaveChangesAsync(ct);

        return ToResponse(todo);
    }

    public async Task<bool> UpdateAsync(
        int id, string title, bool isDone, DateOnly? dueDate, Priority priority, CancellationToken ct)
    {
        var todo = await todos.FindAsync(id, user.Id, ct);
        if (todo is null) return false;

        todo.Update(title, isDone, dueDate, priority);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var todo = await todos.FindAsync(id, user.Id, ct);
        if (todo is null) return false;

        todos.Remove(todo);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    /// <param name="today">
    /// Локальная дата клиента (чтобы «просрочено» не зависело от часового пояса сервера);
    /// без неё берётся текущая дата UTC.
    /// </param>
    public Task<TodoStatsResponse> GetStatsAsync(DateOnly? today, CancellationToken ct) =>
        todos.GetStatsAsync(user.Id, today ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime), ct);

    private static TodoResponse ToResponse(TodoItem t) =>
        new(t.Id, t.Title, t.IsDone, t.CreatedAt, t.DueDate, t.Priority);
}
