namespace MyApp.Domain.Todos;

public class TodoItem
{
    public const int MaxTitleLength = 200;

    private TodoItem() { }                      // для EF Core

    public TodoItem(
        string userId, string title, DateTime dueAtUtc, Priority priority, DateTime createdAtUtc,
        string? assigneeId = null, int? projectId = null)
    {
        UserId = userId;
        Title = NormalizeTitle(title);
        DueAt = AsUtc(dueAtUtc);
        Priority = priority;
        CreatedAt = createdAtUtc;
        AssigneeId = NormalizeId(assigneeId);
        ProjectId = projectId;
    }

    public int Id { get; private set; }

    /// <summary>Автор задачи (владелец).</summary>
    public string UserId { get; private set; } = "";
    public string Title { get; private set; } = "";
    public bool IsDone { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>Срок: дата и время (UTC). Если при создании не указан, берётся время создания.</summary>
    public DateTime DueAt { get; private set; }
    public Priority Priority { get; private set; }

    /// <summary>Исполнитель; null — не назначен.</summary>
    public string? AssigneeId { get; private set; }

    /// <summary>Проект; null — задача без проекта. Проект после создания не меняется.</summary>
    public int? ProjectId { get; private set; }

    public void Update(string title, DateTime dueAtUtc, Priority priority, string? assigneeId)
    {
        Title = NormalizeTitle(title);
        DueAt = AsUtc(dueAtUtc);
        Priority = priority;
        AssigneeId = NormalizeId(assigneeId);
    }

    public void SetDone(bool isDone) => IsDone = isDone;

    /// <summary>Любое время приводится к UTC (Npgsql принимает для timestamptz только UTC).</summary>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string? NormalizeId(string? id) => string.IsNullOrWhiteSpace(id) ? null : id.Trim();

    private static string NormalizeTitle(string? title)
    {
        var t = title?.Trim() ?? "";
        if (t.Length < 1 || t.Length > MaxTitleLength)
            throw new ArgumentException(
                $"Название должно содержать от 1 до {MaxTitleLength} символов", nameof(title));
        return t;
    }
}
