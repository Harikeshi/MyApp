using System.Net;
using System.Net.Http.Json;
using MyApp.Application.Common;
using MyApp.Application.Todos;

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

        var list = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos", TestJson.Options);
        var item = Assert.Single(list!.Items);
        Assert.Equal("Купить молоко", item.Title);
        Assert.False(item.IsDone);
    }

    [Fact]
    public async Task List_is_split_into_pages()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        for (var i = 1; i <= 3; i++)
            await client.PostAsJsonAsync("/todos", new { title = $"задача {i}" });

        var first = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos?page=1&pageSize=2", TestJson.Options);
        var second = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos?page=2&pageSize=2", TestJson.Options);

        Assert.Equal(2, first!.Items.Count);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Single(second!.Items);
        Assert.Empty(first.Items.Select(t => t.Id).Intersect(second.Items.Select(t => t.Id)));   // страницы не пересекаются
    }

    [Fact]
    public async Task List_page_size_is_capped_and_page_past_the_end_is_empty()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/todos", new { title = "одна" });

        var capped = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos?pageSize=1000", TestJson.Options);
        var past = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos?page=5", TestJson.Options);

        Assert.Equal(PageRequest.MaxPageSize, capped!.PageSize);
        Assert.Empty(past!.Items);
        Assert.Equal(1, past.TotalCount);
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

        var bobList = await bob.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos", TestJson.Options);
        Assert.Empty(bobList!.Items);

        var delete = await bob.DeleteAsync($"/todos/{todo!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);        

        var aliceList = await alice.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos", TestJson.Options);
        Assert.Single(aliceList!.Items);
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
        Assert.Equal(1, stats.NormalPriorityOpen);
        Assert.Equal(0, stats.LowPriorityOpen);
    }

    [Fact]
    public async Task Stats_overdue_uses_the_day_sent_by_the_client()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/todos", new { title = "срок 2026-11-14", dueDate = "2026-11-14" });

        var before = await client.GetFromJsonAsync<TodoStatsResponse>("/todos/stats?today=2026-11-14", TestJson.Options);
        var after = await client.GetFromJsonAsync<TodoStatsResponse>("/todos/stats?today=2026-11-15", TestJson.Options);

        Assert.Equal(0, before!.Overdue);     // в день срока ещё не просрочено
        Assert.Equal(1, after!.Overdue);      // на следующий день просрочено
    }
}