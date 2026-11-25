using MyApp.Domain.Notes;
using MyApp.Domain.Todos;

namespace MyApp.Tests.Domain;

public class ProjectAndNoteTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Project_members_exclude_owner_and_duplicates()
    {
        var project = new Project("owner", "  План  ", Now);

        project.SetMembers(["a", "b", "a", "owner", " "]);

        Assert.Equal("План", project.Name);
        Assert.Equal(new[] { "a", "b" }, project.Members.Select(m => m.UserId).Order());
        Assert.True(project.IsParticipant("owner"));
        Assert.True(project.IsParticipant("a"));
        Assert.False(project.IsParticipant("c"));
    }

    [Fact]
    public void Project_set_members_replaces_the_list()
    {
        var project = new Project("owner", "План", Now);
        project.SetMembers(["a", "b"]);

        project.SetMembers(["b", "c"]);

        Assert.Equal(new[] { "b", "c" }, project.Members.Select(m => m.UserId).Order());
    }

    [Fact]
    public void Project_with_empty_name_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Project("owner", "  ", Now));
    }

    [Fact]
    public void Note_plain_text_has_no_markup_or_images()
    {
        var html = "<p>Привет, <span style=\"color:#f00\">мир</span></p><img src=\"data:image/png;base64,AAAA\"><div>&amp; ещё</div>";

        var note = new Note("u1", "Заметка", NoteTag.Faq, html, Now);

        Assert.Equal("Привет, мир & ещё", note.PlainText);
        Assert.Equal(html, note.Content);
    }

    [Fact]
    public void Note_update_changes_fields_and_time()
    {
        var note = new Note("u1", "a", NoteTag.Task, "<p>x</p>", Now);
        var later = Now.AddHours(1);

        note.Update("b", NoteTag.Faq, "<p>y</p>", later);

        Assert.Equal("b", note.Title);
        Assert.Equal(NoteTag.Faq, note.Tag);
        Assert.Equal("y", note.PlainText);
        Assert.Equal(Now, note.CreatedAt);
        Assert.Equal(later, note.UpdatedAt);
    }

    [Fact]
    public void Note_with_empty_title_or_huge_content_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new Note("u1", " ", NoteTag.Task, "", Now));
        Assert.Throws<ArgumentException>(() =>
            new Note("u1", "a", NoteTag.Task, new string('x', Note.MaxContentLength + 1), Now));
    }
}
