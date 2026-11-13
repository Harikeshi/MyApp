using MyApp.Domain.Todos;

namespace MyApp.Tests.Domain;

public class TodoItemTests
{
    [Fact]
    public void Title_is_trimmed()
    {
        var todo = new TodoItem("u1", "  Купить молоко  ", null, Priority.Normal, DateTime.UtcNow);

        Assert.Equal("Купить молоко", todo.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_title_is_rejected(string title)
    {
        Assert.Throws<ArgumentException>(() =>
            new TodoItem("u1", title, null, Priority.Normal, DateTime.UtcNow));
    }

    [Fact]
    public void Too_long_title_is_rejected()
    {
        var title = new string('x', TodoItem.MaxTitleLength + 1);

        Assert.Throws<ArgumentException>(() =>
            new TodoItem("u1", title, null, Priority.Normal, DateTime.UtcNow));
    }

    [Fact]
    public void Update_changes_all_fields()
    {
        var todo = new TodoItem("u1", "a", null, Priority.Low, DateTime.UtcNow);

        todo.Update("b", true, new DateOnly(2027, 1, 1), Priority.High);

        Assert.Equal("b", todo.Title);
        Assert.True(todo.IsDone);
        Assert.Equal(new DateOnly(2027, 1, 1), todo.DueDate);
        Assert.Equal(Priority.High, todo.Priority);
    }
    
}
