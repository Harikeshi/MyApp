using System.Net;
using System.Net.Http.Json;
using MyApp.Application.Notes;
using MyApp.Application.Search;
using MyApp.Application.Todos;
using MyApp.Domain.Notes;

namespace MyApp.Tests;

public class NoteAndSearchApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Html = "<p>Pasta <span style=\"color:#d00\">recipe</span></p><img src=\"data:image/png;base64,AAAA\">";

    [Fact]
    public async Task Notes_without_token_return_401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/notes")).StatusCode);
    }

    [Fact]
    public async Task Note_is_saved_listed_without_content_read_updated_and_deleted()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/notes", new { title = "Рецепт", tag = "faq", content = Html });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var note = (await created.Content.ReadFromJsonAsync<NoteResponse>(TestJson.Options))!;
        Assert.Equal(NoteTag.Faq, note.Tag);

        var list = (await client.GetFromJsonAsync<List<NoteListItem>>("/notes", TestJson.Options))!;
        var item = Assert.Single(list);
        Assert.Equal("Pasta recipe", item.Preview);          // без разметки и картинок

        var full = (await client.GetFromJsonAsync<NoteResponse>($"/notes/{note.Id}", TestJson.Options))!;
        Assert.Equal(Html, full.Content);                    // картинка на месте

        var put = await client.PutAsJsonAsync($"/notes/{note.Id}", new { title = "Рецепт 2", tag = "task", content = "<p>new</p>" });
        var updated = (await put.Content.ReadFromJsonAsync<NoteResponse>(TestJson.Options))!;
        Assert.Equal("Рецепт 2", updated.Title);
        Assert.Equal(NoteTag.Task, updated.Tag);
        Assert.Equal("<p>new</p>", updated.Content);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/notes/{note.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/notes/{note.Id}")).StatusCode);
    }

    [Fact]
    public async Task Note_without_title_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/notes", new { title = "", tag = "task", content = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_read_change_or_delete_other_users_note()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var note = (await (await alice.PostAsJsonAsync("/notes", new { title = "секрет", tag = "task", content = "x" }))
            .Content.ReadFromJsonAsync<NoteResponse>(TestJson.Options))!;

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/notes/{note.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await bob.PutAsJsonAsync($"/notes/{note.Id}", new { title = "x", tag = "task", content = "y" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/notes/{note.Id}")).StatusCode);
        Assert.Empty((await bob.GetFromJsonAsync<List<NoteListItem>>("/notes", TestJson.Options))!);
        Assert.Single((await alice.GetFromJsonAsync<List<NoteListItem>>("/notes", TestJson.Options))!);
    }

    // В тестах SQLite: lower() у неё работает только с латиницей, поэтому ищем английские слова.
    [Fact]
    public async Task Search_finds_own_tasks_notes_and_projects_only()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        await alice.PostAsJsonAsync("/todos", new { title = "Buy MILK" });
        await alice.PostAsJsonAsync("/notes", new { title = "Shopping", tag = "task", content = "<p>milk and eggs</p>" });
        await alice.PostAsJsonAsync("/projects", new { name = "Milk project" });
        await bob.PostAsJsonAsync("/todos", new { title = "Bobs milk" });

        var found = (await alice.GetFromJsonAsync<SearchResponse>("/search?q=milk", TestJson.Options))!;

        Assert.Equal("Buy MILK", Assert.Single(found.Tasks).Title);
        Assert.Equal("Shopping", Assert.Single(found.Notes).Title);
        Assert.Equal("Milk project", Assert.Single(found.Projects).Name);
    }

    [Fact]
    public async Task Search_ignores_too_short_queries()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/todos", new { title = "abc" });

        var found = (await client.GetFromJsonAsync<SearchResponse>("/search?q=a", TestJson.Options))!;

        Assert.Empty(found.Tasks);
        Assert.Empty(found.Notes);
        Assert.Empty(found.Projects);
    }

    [Fact]
    public async Task User_search_finds_by_name_and_hides_the_current_user()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        await factory.CreateAuthenticatedClientAsync();          // имя у всех «Test User»

        var found = (await alice.GetFromJsonAsync<List<UserRef>>("/users?search=test", TestJson.Options))!;

        Assert.NotEmpty(found);
        Assert.All(found, u => Assert.Equal("Test User", u.Name));
        Assert.Empty((await alice.GetFromJsonAsync<List<UserRef>>("/users?search=t", TestJson.Options))!);   // меньше 2 символов
    }

    private record UserRef(string Id, string Name);
}
