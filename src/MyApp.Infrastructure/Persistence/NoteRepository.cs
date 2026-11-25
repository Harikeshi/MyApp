using Microsoft.EntityFrameworkCore;
using MyApp.Application.Abstractions;
using MyApp.Application.Notes;
using MyApp.Domain.Notes;

namespace MyApp.Infrastructure.Persistence;

public class NoteRepository(AppDbContext db) : INoteRepository
{
    public const int PreviewLength = 120;

    public async Task<IReadOnlyList<NoteListItem>> ListAsync(string userId, int take, CancellationToken ct) =>
        await db.Notes.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.UpdatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            // содержимое с картинками в список не попадает: берём только начало обычного текста
            .Select(n => new NoteListItem(n.Id, n.Title, n.Tag, n.UpdatedAt, n.PlainText.Substring(0, PreviewLength)))
            .ToListAsync(ct);

    public Task<Note?> FindAsync(int id, string userId, CancellationToken ct) =>
        db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

    public void Add(Note note) => db.Notes.Add(note);

    public void Remove(Note note) => db.Notes.Remove(note);
}
