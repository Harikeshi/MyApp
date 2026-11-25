using MyApp.Domain.Notes;

namespace MyApp.Application.Notes;

/// <summary>Строка списка записей: без содержимого (в нём могут быть картинки).</summary>
public record NoteListItem(int Id, string Title, NoteTag Tag, DateTime UpdatedAt, string Preview);

public record NoteResponse(int Id, string Title, NoteTag Tag, string Content, DateTime CreatedAt, DateTime UpdatedAt);
