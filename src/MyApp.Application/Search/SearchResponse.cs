using MyApp.Domain.Notes;

namespace MyApp.Application.Search;

public record SearchTask(int Id, string Title, int? ProjectId, bool IsDone);

public record SearchNote(int Id, string Title, NoteTag Tag, string Preview);

public record SearchProject(int Id, string Name);

/// <summary>Результаты поиска по данным пользователя. Чаты живут в сервисе AiChat и ищутся отдельно.</summary>
public record SearchResponse(
    IReadOnlyList<SearchTask> Tasks,
    IReadOnlyList<SearchNote> Notes,
    IReadOnlyList<SearchProject> Projects)
{
    public static SearchResponse Empty { get; } = new([], [], []);
}
