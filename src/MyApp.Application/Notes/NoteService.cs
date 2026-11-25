using MyApp.Application.Abstractions;
using MyApp.Domain.Notes;

namespace MyApp.Application.Notes;

/// <summary>Записи видны и доступны только их автору: чужая запись для пользователя «не найдена».</summary>
public class NoteService(INoteRepository notes, IUnitOfWork unitOfWork, ICurrentUser user, TimeProvider clock)
{
    public const int MaxListed = 1000;

    public Task<IReadOnlyList<NoteListItem>> ListAsync(CancellationToken ct) =>
        notes.ListAsync(user.Id, MaxListed, ct);

    public async Task<NoteResponse?> GetAsync(int id, CancellationToken ct)
    {
        var note = await notes.FindAsync(id, user.Id, ct);
        return note is null ? null : ToResponse(note);
    }

    public async Task<NoteResponse> CreateAsync(string title, NoteTag tag, string content, CancellationToken ct)
    {
        var note = new Note(user.Id, title, tag, content, clock.GetUtcNow().UtcDateTime);

        notes.Add(note);
        await unitOfWork.SaveChangesAsync(ct);
        return ToResponse(note);
    }

    public async Task<NoteResponse?> UpdateAsync(int id, string title, NoteTag tag, string content, CancellationToken ct)
    {
        var note = await notes.FindAsync(id, user.Id, ct);
        if (note is null) return null;

        note.Update(title, tag, content, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        return ToResponse(note);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var note = await notes.FindAsync(id, user.Id, ct);
        if (note is null) return false;

        notes.Remove(note);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    private static NoteResponse ToResponse(Note n) => new(n.Id, n.Title, n.Tag, n.Content, n.CreatedAt, n.UpdatedAt);
}
