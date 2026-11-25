using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Domain.Todos;

namespace MyApp.Infrastructure.Persistence;

public class ProjectRepository(AppDbContext db) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> ListVisibleAsync(string userId, CancellationToken ct) =>
        await db.Projects.AsNoTracking()
            .Include(p => p.Members)
            .VisibleTo(userId)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);

    public Task<Project?> FindVisibleAsync(int id, string userId, CancellationToken ct) =>
        db.Projects
            .Include(p => p.Members)
            .VisibleTo(userId)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(Project project) => db.Projects.Add(project);

    public void Remove(Project project) => db.Projects.Remove(project);
}
