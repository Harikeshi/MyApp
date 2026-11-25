using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Application.Search;

namespace MyApp.Infrastructure.Persistence;

public class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<SearchResponse> SearchAsync(string userId, string query, int take, CancellationToken ct)
    {
        var q = query.ToLowerInvariant();

        // Запросы идут по очереди: один DbContext не допускает параллельных.
        var tasks = await db.Todos.AsNoTracking()
            .VisibleTo(db, userId)
            .Where(t => t.Title.ToLower().Contains(q))
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Id)
            .Take(take)
            .Select(t => new SearchTask(t.Id, t.Title, t.ProjectId, t.IsDone))
            .ToListAsync(ct);

        var notes = await db.Notes.AsNoTracking()
            .Where(n => n.UserId == userId && (n.Title.ToLower().Contains(q) || n.PlainText.ToLower().Contains(q)))
            .OrderByDescending(n => n.UpdatedAt)
            .Take(take)
            .Select(n => new SearchNote(n.Id, n.Title, n.Tag, n.PlainText.Substring(0, NoteRepository.PreviewLength)))
            .ToListAsync(ct);

        var projects = await db.Projects.AsNoTracking()
            .VisibleTo(userId)
            .Where(p => p.Name.ToLower().Contains(q))
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new SearchProject(p.Id, p.Name))
            .ToListAsync(ct);

        return new SearchResponse(tasks, notes, projects);
    }
}
