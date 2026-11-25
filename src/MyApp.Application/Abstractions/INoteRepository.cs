using MyApp.Application.Notes;
using MyApp.Domain.Notes;

namespace MyApp.Application.Abstractions;

public interface INoteRepository
{
    /// <summary>Записи пользователя, свежие сверху. Без содержимого: только заголовок и превью.</summary>
    Task<IReadOnlyList<NoteListItem>> ListAsync(string userId, int take, CancellationToken ct);

    /// <summary>Запись по id (с отслеживанием), только если она принадлежит пользователю.</summary>
    Task<Note?> FindAsync(int id, string userId, CancellationToken ct);

    void Add(Note note);

    void Remove(Note note);
}
