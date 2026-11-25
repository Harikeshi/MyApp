using MyApp.Domain.Todos;

namespace MyApp.Tests.Domain;

public class TodoItemTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Title_is_trimmed()
    {
        var todo = new TodoItem("u1", "  Купить молоко  ", Now, Priority.Normal, Now);

        Assert.Equal("Купить молоко", todo.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_title_is_rejected(string title)
    {
        Assert.Throws<ArgumentException>(() => new TodoItem("u1", title, Now, Priority.Normal, Now));
    }

    [Fact]
    public void Too_long_title_is_rejected()
    {
        var title = new string('x', TodoItem.MaxTitleLength + 1);

        Assert.Throws<ArgumentException>(() => new TodoItem("u1", title, Now, Priority.Normal, Now));
    }

    [Fact]
    public void Update_changes_text_due_priority_and_assignee_but_not_done_or_project()
    {
        var todo = new TodoItem("u1", "a", Now, Priority.Low, Now, assigneeId: "u2", projectId: 7);
        todo.SetDone(true);

        var due = Now.AddDays(3);
        todo.Update("b", due, Priority.High, "u3");

        Assert.Equal("b", todo.Title);
        Assert.Equal(due, todo.DueAt);
        Assert.Equal(Priority.High, todo.Priority);
        Assert.Equal("u3", todo.AssigneeId);
        Assert.True(todo.IsDone);
        Assert.Equal(7, todo.ProjectId);
    }

    [Fact]
    public void Blank_assignee_means_no_assignee()
    {
        var todo = new TodoItem("u1", "a", Now, Priority.Normal, Now, assigneeId: "  ");

        Assert.Null(todo.AssigneeId);
    }

    [Fact]
    public void Due_time_is_stored_as_utc()
    {
        var local = new DateTime(2026, 11, 14, 15, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 11, 14, 15, 0, 0, DateTimeKind.Unspecified);

        var a = new TodoItem("u1", "a", local, Priority.Normal, Now);
        var b = new TodoItem("u1", "b", unspecified, Priority.Normal, Now);

        Assert.Equal(DateTimeKind.Utc, a.DueAt.Kind);
        Assert.Equal(local.ToUniversalTime(), a.DueAt);
        Assert.Equal(DateTimeKind.Utc, b.DueAt.Kind);
        Assert.Equal(15, b.DueAt.Hour);            // время без пояса считается UTC как есть
    }
}
