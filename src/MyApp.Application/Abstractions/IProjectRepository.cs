using MyApp.Domain.Todos;

namespace MyApp.Application.Abstractions;

public interface IProjectRepository
{
    /// <summary>Проекты, где пользователь владелец или ответственный (с участниками).</summary>
    Task<IReadOnlyList<Project>> ListVisibleAsync(string userId, CancellationToken ct);

    /// <summary>Проект с участниками (с отслеживанием), если он виден пользователю.</summary>
    Task<Project?> FindVisibleAsync(int id, string userId, CancellationToken ct);

    void Add(Project project);

    void Remove(Project project);
}
