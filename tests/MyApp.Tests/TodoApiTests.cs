using System.Net;
using System.Net.Http.Json;
using MyApp.Application.Todos;

namespace MyApp.Tests;

public class TodoApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_endpoints_return_200()
    {
        var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

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

        var list = await client.GetFromJsonAsync<List<TodoResponse>>("/todos", TestJson.Options);
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
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);

        var bobList = await bob.GetFromJsonAsync<List<TodoResponse>>("/todos", TestJson.Options);
        Assert.Empty(bobList!);

        var delete = await bob.DeleteAsync($"/todos/{todo!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);        

        var aliceList = await alice.GetFromJsonAsync<List<TodoResponse>>("/todos", TestJson.Options);
        Assert.Single(aliceList!);
    }
    
    [Fact]
    public async Task Stats_count_open_done_and_high_priority()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/todos", new { title = "a", priority = "high" });
        await client.PostAsJsonAsync("/todos", new { title = "b" });

        var stats = await client.GetFromJsonAsync<TodoStatsResponse>("/todos/stats", TestJson.Options);

        Assert.Equal(2, stats!.Total);
        Assert.Equal(2, stats.Open);
        Assert.Equal(1, stats.HighPriorityOpen);
    }
}