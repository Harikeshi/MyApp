using MyApp.Domain.Todos;

namespace MyApp.Infrastructure.Persistence;

/// <summary>
/// Кто что видит. Задача видна автору, исполнителю и тем, кто владелец или ответственный проекта задачи.
/// Проект виден владельцу и ответственным. Условия в одном месте: их же использует поиск.
/// </summary>
internal static class Visibility
{
    public static IQueryable<TodoItem> VisibleTo(this IQueryable<TodoItem> todos, AppDbContext db, string userId) =>
        todos.Where(t =>
            t.UserId == userId
            || t.AssigneeId == userId
            || (t.ProjectId != null && db.Projects.Any(p =>
                p.Id == t.ProjectId && (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)))));

    public static IQueryable<Project> VisibleTo(this IQueryable<Project> projects, string userId) =>
        projects.Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId));
}
