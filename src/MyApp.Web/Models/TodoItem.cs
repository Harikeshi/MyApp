namespace MyApp.Web.Models;

public enum Priority { Low = -1, Normal = 0, High = 1 }
public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public Priority Priority { get; set; } = Priority.Normal;
}
