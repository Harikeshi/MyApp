using System.Net;
using System.Net.Http.Json;
using MyApp.Web.Models;

namespace MyApp.Tests;

public class TodoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task List_without_token_returns_401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/todos");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_and_list_returns_created_todo()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/todos", new { title = "Купить молоко" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<List<TodoResponse>>("/todos");
        var item = Assert.Single(list!);
        Assert.Equal("Купить молоко", item.Title);
        Assert.False(item.IsDone);
    }

    [Fact]
    public async Task Create_with_empty_title_returns_400()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/todos", new { title = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_see_or_delete_other_users_todo()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();

        var created = await alice.PostAsJsonAsync("/todos", new { title = "Задача Алисы" });
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>();

        var bobList = await bob.GetFromJsonAsync<List<TodoResponse>>("/todos");
        Assert.Empty(bobList!);

        var delete = await bob.DeleteAsync($"/todos/{todo!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var aliceList = await alice.GetFromJsonAsync<List<TodoResponse>>("/todos");
        Assert.Single(aliceList!);
    }
}