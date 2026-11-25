using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

public class TodoService(
    ITodoRepository todos,
    IProjectRepository projects,
    IUserDirectory users,
    IUnitOfWork unitOfWork,
    ICurrentUser user,
    TimeProvider clock)
{
    private const string UnknownUser = "Пользователь";
    private const string AssigneeError = "Исполнителем может быть только вы или участник проекта.";

    /// <param name="projectId">Только задачи этого проекта; null — все видимые задачи.</param>
    /// <param name="page">Номер страницы с 1 (по умолчанию 1).</param>
    /// <param name="pageSize">Размер страницы (по умолчанию 20, не больше 100); неверные значения приводятся к допустимым.</param>
    public async Task<PagedResult<TodoResponse>> ListAsync(
        int? projectId, int? page, int? pageSize, CancellationToken ct)
    {
        var result = await todos.ListAsync(user.Id, projectId, PageRequest.Of(page, pageSize), ct);
        var names = await NamesAsync(result.Items.Select(e => e.Todo), ct);
        return result.Map(e => ToResponse(e, names));
    }

    /// <param name="dueAt">Срок; не указан — текущее время.</param>
    public async Task<Outcome<TodoResponse>> CreateAsync(
        string title, DateTime? dueAt, Priority priority, string? assigneeId, int? projectId, CancellationToken ct)
    {
        Project? project = null;
        if (projectId is int pid)
        {
            project = await projects.FindVisibleAsync(pid, user.Id, ct);
            if (project is null) return Outcome<TodoResponse>.NotFound();
        }

        var assignee = NormalizeAssignee(assigneeId);
        if (!CanAssign(assignee, project))
            return Outcome<TodoResponse>.Invalid(AssigneeError);

        var now = clock.GetUtcNow().UtcDateTime;
        var todo = new TodoItem(user.Id, title, dueAt ?? now, priority, now, assignee, project?.Id);

        todos.Add(todo);
        await unitOfWork.SaveChangesAsync(ct);

        return Outcome<TodoResponse>.Ok(await ToResponseAsync(new TodoEntry(todo, project?.OwnerId), ct));
    }

    /// <summary>Меняет текст, срок, приоритет и исполнителя. Может автор задачи или владелец проекта.</summary>
    public async Task<Outcome<TodoResponse>> UpdateAsync(
        int id, string title, DateTime? dueAt, Priority priority, string? assigneeId, CancellationToken ct)
    {
        var entry = await todos.FindAsync(id, user.Id, ct);
        if (entry is null) return Outcome<TodoResponse>.NotFound();
        if (!CanManage(entry))
            return Outcome<TodoResponse>.Forbidden("Менять задачу может её автор или владелец проекта.");

        var todo = entry.Todo;
        var assignee = NormalizeAssignee(assigneeId);
        if (assignee != todo.AssigneeId)             // прежнего исполнителя не перепроверяем: он мог выйти из проекта
        {
            var project = todo.ProjectId is int pid ? await projects.FindVisibleAsync(pid, user.Id, ct) : null;
            if (!CanAssign(assignee, project))
                return Outcome<TodoResponse>.Invalid(AssigneeError);
        }

        todo.Update(title, dueAt ?? todo.DueAt, priority, assignee);
        await unitOfWork.SaveChangesAsync(ct);

        return Outcome<TodoResponse>.Ok(await ToResponseAsync(entry, ct));
    }

    /// <summary>Отметка «выполнено»: доступна всем, кто видит задачу (в том числе исполнителю).</summary>
    public async Task<Outcome<TodoResponse>> SetDoneAsync(int id, bool isDone, CancellationToken ct)
    {
        var entry = await todos.FindAsync(id, user.Id, ct);
        if (entry is null) return Outcome<TodoResponse>.NotFound();

        entry.Todo.SetDone(isDone);
        await unitOfWork.SaveChangesAsync(ct);

        return Outcome<TodoResponse>.Ok(await ToResponseAsync(entry, ct));
    }

    public async Task<Outcome<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var entry = await todos.FindAsync(id, user.Id, ct);
        if (entry is null) return Outcome<bool>.NotFound();
        if (!CanManage(entry))
            return Outcome<bool>.Forbidden("Удалить задачу может её автор или владелец проекта.");

        todos.Remove(entry.Todo);
        await unitOfWork.SaveChangesAsync(ct);
        return Outcome<bool>.Ok(true);
    }

    /// <param name="todayStart">
    /// Начало текущего дня клиента (чтобы «просрочено» не зависело от часового пояса сервера);
    /// без него берётся начало текущих суток UTC.
    /// </param>
    public Task<TodoStatsResponse> GetStatsAsync(DateTimeOffset? todayStart, CancellationToken ct) =>
        todos.GetStatsAsync(user.Id, todayStart?.UtcDateTime ?? clock.GetUtcNow().UtcDateTime.Date, ct);

    private bool CanManage(TodoEntry e) => e.Todo.UserId == user.Id || e.ProjectOwnerId == user.Id;

    private static string? NormalizeAssignee(string? id) => string.IsNullOrWhiteSpace(id) ? null : id.Trim();

    /// <summary>Исполнитель: никто, сам пользователь или участник проекта. Остальных назначать нельзя (спам задачами).</summary>
    private bool CanAssign(string? assigneeId, Project? project) =>
        assigneeId is null || assigneeId == user.Id || project?.IsParticipant(assigneeId) == true;

    private async Task<IReadOnlyDictionary<string, string>> NamesAsync(IEnumerable<TodoItem> items, CancellationToken ct)
    {
        var ids = items
            .SelectMany(t => new[] { t.UserId, t.AssigneeId })
            .OfType<string>()
            .Distinct()
            .ToList();
        return ids.Count == 0 ? new Dictionary<string, string>() : await users.GetNamesAsync(ids, ct);
    }

    private async Task<TodoResponse> ToResponseAsync(TodoEntry entry, CancellationToken ct) =>
        ToResponse(entry, await NamesAsync([entry.Todo], ct));

    private TodoResponse ToResponse(TodoEntry e, IReadOnlyDictionary<string, string> names)
    {
        var t = e.Todo;
        return new TodoResponse(
            t.Id, t.Title, t.IsDone, t.CreatedAt, t.DueAt, t.Priority, t.ProjectId,
            t.UserId, names.GetValueOrDefault(t.UserId, UnknownUser),
            t.AssigneeId, t.AssigneeId is null ? null : names.GetValueOrDefault(t.AssigneeId, UnknownUser),
            CanManage(e));
    }
}
