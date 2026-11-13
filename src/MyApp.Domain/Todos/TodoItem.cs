namespace MyApp.Domain.Todos;

public class TodoItem
{
    public const int MaxTitleLength = 200;

    private TodoItem() { }                      // для EF Core

    public TodoItem(string userId, string title, DateOnly? dueDate, Priority priority, DateTime createdAtUtc)
    {
        UserId = userId;
        Title = NormalizeTitle(title);
        DueDate = dueDate;
        Priority = priority;
        CreatedAt = createdAtUtc;
    }

    public int Id { get; private set; }
    public string UserId { get; private set; } = "";
    public string Title { get; private set; } = "";
    public bool IsDone { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Priority Priority { get; private set; }

    public void Update(string title, bool isDone, DateOnly? dueDate, Priority priority)
    {
        Title = NormalizeTitle(title);
        IsDone = isDone;
        DueDate = dueDate;
        Priority = priority;
    }

    private static string NormalizeTitle(string? title)
    {
        var t = title?.Trim() ?? "";
        if (t.Length < 1 || t.Length > MaxTitleLength)
            throw new ArgumentException(
                $"Название должно содержать от 1 до {MaxTitleLength} символов", nameof(title));
        return t;
    }
}
