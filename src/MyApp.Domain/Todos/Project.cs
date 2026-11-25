namespace MyApp.Domain.Todos;

/// <summary>Проект: владелец, название и ответственные (участники). В проект входят задачи.</summary>
public class Project
{
    public const int MaxNameLength = 100;
    public const int MaxMembers = 50;

    private readonly List<ProjectMember> _members = [];

    private Project() { }                       // для EF Core

    public Project(string ownerId, string name, DateTime createdAtUtc)
    {
        OwnerId = ownerId;
        Name = NormalizeName(name);
        CreatedAt = createdAtUtc;
    }

    public int Id { get; private set; }
    public string OwnerId { get; private set; } = "";
    public string Name { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }

    /// <summary>Ответственные (без владельца).</summary>
    public IReadOnlyCollection<ProjectMember> Members => _members;

    /// <summary>Владелец или ответственный: такой пользователь видит проект и его задачи.</summary>
    public bool IsParticipant(string userId) => OwnerId == userId || _members.Any(m => m.UserId == userId);

    public void Rename(string name) => Name = NormalizeName(name);

    /// <summary>Заменяет список ответственных. Владелец и повторы отбрасываются.</summary>
    public void SetMembers(IEnumerable<string> userIds)
    {
        var wanted = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id) && id != OwnerId)
            .Select(id => id.Trim())
            .ToHashSet();
        if (wanted.Count > MaxMembers)
            throw new ArgumentException($"В проекте не может быть больше {MaxMembers} ответственных", nameof(userIds));

        _members.RemoveAll(m => !wanted.Contains(m.UserId));
        foreach (var id in wanted)
            if (_members.All(m => m.UserId != id))
                _members.Add(new ProjectMember(id));
    }

    private static string NormalizeName(string? name)
    {
        var n = name?.Trim() ?? "";
        if (n.Length < 1 || n.Length > MaxNameLength)
            throw new ArgumentException(
                $"Название проекта должно содержать от 1 до {MaxNameLength} символов", nameof(name));
        return n;
    }
}

public class ProjectMember
{
    private ProjectMember() { }                 // для EF Core

    public ProjectMember(string userId) => UserId = userId;

    public int ProjectId { get; private set; }
    public string UserId { get; private set; } = "";
}
