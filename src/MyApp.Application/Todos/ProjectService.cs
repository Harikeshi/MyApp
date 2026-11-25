using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Domain.Todos;

namespace MyApp.Application.Todos;

public class ProjectService(
    IProjectRepository projects,
    IUserDirectory users,
    IUnitOfWork unitOfWork,
    ICurrentUser user,
    TimeProvider clock)
{
    private const string UnknownUser = "Пользователь";
    private const int MinSearchLength = 2;
    private const int SearchLimit = 10;

    /// <summary>Проекты, в которых пользователь владелец или ответственный.</summary>
    public async Task<IReadOnlyList<ProjectResponse>> ListAsync(CancellationToken ct)
    {
        var list = await projects.ListVisibleAsync(user.Id, ct);
        var names = await NamesAsync(list, ct);
        return list.Select(p => ToResponse(p, names)).ToList();
    }

    public async Task<Outcome<ProjectResponse>> CreateAsync(
        string name, IReadOnlyCollection<string> memberIds, CancellationToken ct)
    {
        var members = await ValidMembersAsync(memberIds, ct);
        if (members is null) return Outcome<ProjectResponse>.Invalid("Некоторые ответственные не найдены.");

        var project = new Project(user.Id, name, clock.GetUtcNow().UtcDateTime);
        project.SetMembers(members);

        projects.Add(project);
        await unitOfWork.SaveChangesAsync(ct);

        return Outcome<ProjectResponse>.Ok(await ToResponseAsync(project, ct));
    }

    /// <summary>Название и ответственные. Только владелец.</summary>
    public async Task<Outcome<ProjectResponse>> UpdateAsync(
        int id, string name, IReadOnlyCollection<string> memberIds, CancellationToken ct)
    {
        var project = await projects.FindVisibleAsync(id, user.Id, ct);
        if (project is null) return Outcome<ProjectResponse>.NotFound();
        if (project.OwnerId != user.Id)
            return Outcome<ProjectResponse>.Forbidden("Менять проект может только его владелец.");

        var members = await ValidMembersAsync(memberIds, ct);
        if (members is null) return Outcome<ProjectResponse>.Invalid("Некоторые ответственные не найдены.");

        project.Rename(name);
        project.SetMembers(members);
        await unitOfWork.SaveChangesAsync(ct);

        return Outcome<ProjectResponse>.Ok(await ToResponseAsync(project, ct));
    }

    /// <summary>Удаляет проект вместе с его задачами (внешний ключ Todos.ProjectId ... ON DELETE CASCADE). Только владелец.</summary>
    public async Task<Outcome<bool>> DeleteAsync(int id, CancellationToken ct)
    {
        var project = await projects.FindVisibleAsync(id, user.Id, ct);
        if (project is null) return Outcome<bool>.NotFound();
        if (project.OwnerId != user.Id)
            return Outcome<bool>.Forbidden("Удалить проект может только его владелец.");

        projects.Remove(project);
        await unitOfWork.SaveChangesAsync(ct);
        return Outcome<bool>.Ok(true);
    }

    /// <summary>Поиск пользователей для списка ответственных (себя в результатах нет).</summary>
    public async Task<IReadOnlyList<UserInfo>> SearchUsersAsync(string? query, CancellationToken ct)
    {
        var q = query?.Trim() ?? "";
        if (q.Length < MinSearchLength) return [];

        var found = await users.SearchAsync(q.Length > 100 ? q[..100] : q, SearchLimit + 1, ct);
        return found.Where(u => u.Id != user.Id).Take(SearchLimit).ToList();
    }

    /// <summary>Список ответственных без повторов и без себя; null, если какого-то пользователя не существует.</summary>
    private async Task<List<string>?> ValidMembersAsync(IReadOnlyCollection<string> ids, CancellationToken ct)
    {
        var wanted = ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Where(id => id != user.Id)
            .Distinct()
            .ToList();
        if (wanted.Count == 0) return wanted;
        if (wanted.Count > Project.MaxMembers) return null;

        var existing = await users.ExistingIdsAsync(wanted, ct);
        return wanted.All(existing.Contains) ? wanted : null;
    }

    private async Task<IReadOnlyDictionary<string, string>> NamesAsync(IEnumerable<Project> list, CancellationToken ct)
    {
        var ids = list.SelectMany(p => p.Members.Select(m => m.UserId).Append(p.OwnerId)).Distinct().ToList();
        return ids.Count == 0 ? new Dictionary<string, string>() : await users.GetNamesAsync(ids, ct);
    }

    private async Task<ProjectResponse> ToResponseAsync(Project p, CancellationToken ct) =>
        ToResponse(p, await NamesAsync([p], ct));

    private ProjectResponse ToResponse(Project p, IReadOnlyDictionary<string, string> names) => new(
        p.Id,
        p.Name,
        p.OwnerId,
        names.GetValueOrDefault(p.OwnerId, UnknownUser),
        p.OwnerId == user.Id,
        p.Members
            .Select(m => new UserInfo(m.UserId, names.GetValueOrDefault(m.UserId, UnknownUser)))
            .OrderBy(m => m.Name)
            .ToList());
}
