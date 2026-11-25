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
    public async Task Create_and_list_returns_created_todo_with_due_time_defaulting_to_now()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var before = DateTime.UtcNow.AddMinutes(-1);

        var created = await client.PostAsJsonAsync("/todos", new { title = "Купить молоко" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var list = await client.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos", TestJson.Options);
        var item = Assert.Single(list!.Items);
        Assert.Equal("Купить молоко", item.Title);
        Assert.False(item.IsDone);
        Assert.True(item.CanManage);
        Assert.InRange(item.DueAt, before, DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Create_keeps_the_chosen_due_time()
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await client.PostAsJsonAsync("/todos", new { title = "a", dueAt = "2027-03-01T08:30:00Z" });
        var todo = await created.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);

        Assert.Equal(new DateTime(2027, 3, 1, 8, 30, 0, DateTimeKind.Utc), todo!.DueAt.ToUniversalTime());
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
    public async Task Author_can_edit_text_due_priority_and_assignee_and_delete()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await (await client.PostAsJsonAsync("/todos", new { title = "a" }))
            .Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);

        var put = await client.PutAsJsonAsync($"/todos/{created!.Id}", new
        {
            title = "b",
            dueAt = "2027-05-05T10:00:00Z",
            priority = "high",
            assigneeId = created.OwnerId
        });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);
        Assert.Equal("b", updated!.Title);
        Assert.Equal(MyApp.Domain.Todos.Priority.High, updated.Priority);
        Assert.Equal(created.OwnerId, updated.AssigneeId);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/todos/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Cannot_assign_a_todo_to_a_stranger()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var bobsTodo = await (await bob.PostAsJsonAsync("/todos", new { title = "x" }))
            .Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options);

        var response = await alice.PostAsJsonAsync("/todos", new { title = "a", assigneeId = bobsTodo!.OwnerId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Project_members_share_tasks_and_only_owner_manages_the_project()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var carol = await factory.CreateAuthenticatedClientAsync();
        var bobId = (await (await bob.PostAsJsonAsync("/todos", new { title = "id" }))
            .Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options))!.OwnerId;

        var created = await alice.PostAsJsonAsync("/projects", new { name = "Релиз", memberIds = new[] { bobId } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var project = (await created.Content.ReadFromJsonAsync<ProjectResponse>(TestJson.Options))!;
        Assert.True(project.IsOwner);
        Assert.Equal(bobId, Assert.Single(project.Members).Id);

        // проект видят владелец и ответственный, но не посторонний
        Assert.Single((await bob.GetFromJsonAsync<List<ProjectResponse>>("/projects", TestJson.Options))!);
        Assert.Empty((await carol.GetFromJsonAsync<List<ProjectResponse>>("/projects", TestJson.Options))!);
        Assert.False((await bob.GetFromJsonAsync<List<ProjectResponse>>("/projects", TestJson.Options))![0].IsOwner);

        // ответственный добавляет задачу в проект и назначает её на владельца
        var added = await bob.PostAsJsonAsync("/todos",
            new { title = "из проекта", projectId = project.Id, assigneeId = project.OwnerId });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var todo = (await added.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options))!;

        var aliceView = await alice.GetFromJsonAsync<PagedResult<TodoResponse>>($"/todos?projectId={project.Id}", TestJson.Options);
        var seen = Assert.Single(aliceView!.Items);
        Assert.True(seen.CanManage);                      // владелец проекта управляет чужой задачей в нём

        Assert.Equal(HttpStatusCode.NotFound, (await carol.DeleteAsync($"/todos/{todo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await carol.PostAsJsonAsync("/todos", new { title = "x", projectId = project.Id })).StatusCode);

        // ответственный не может менять и удалять проект
        Assert.Equal(HttpStatusCode.Forbidden,
            (await bob.PutAsJsonAsync($"/projects/{project.Id}", new { name = "Другое" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.DeleteAsync($"/projects/{project.Id}")).StatusCode);

        // владелец переименовывает и удаляет проект вместе с задачами
        var renamed = await alice.PutAsJsonAsync($"/projects/{project.Id}", new { name = "Релиз 2", memberIds = Array.Empty<string>() });
        Assert.Equal("Релиз 2", (await renamed.Content.ReadFromJsonAsync<ProjectResponse>(TestJson.Options))!.Name);
        Assert.Empty((await bob.GetFromJsonAsync<List<ProjectResponse>>("/projects", TestJson.Options))!);

        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/projects/{project.Id}")).StatusCode);
        var after = await alice.GetFromJsonAsync<PagedResult<TodoResponse>>("/todos", TestJson.Options);
        Assert.Empty(after!.Items);
    }

    [Fact]
    public async Task Assignee_can_mark_done_a_task_assigned_to_them()
    {
        var alice = await factory.CreateAuthenticatedClientAsync();
        var bob = await factory.CreateAuthenticatedClientAsync();
        var bobId = (await (await bob.PostAsJsonAsync("/todos", new { title = "id" }))
            .Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options))!.OwnerId;
        var project = (await (await alice.PostAsJsonAsync("/projects", new { name = "П", memberIds = new[] { bobId } }))
            .Content.ReadFromJsonAsync<ProjectResponse>(TestJson.Options))!;
        var todo = (await (await alice.PostAsJsonAsync("/todos",
                new { title = "для Боба", projectId = project.Id, assigneeId = bobId }))
            .Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options))!;

        var done = await bob.PatchAsJsonAsync($"/todos/{todo.Id}/done", new { isDone = true });

        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        Assert.True((await done.Content.ReadFromJsonAsync<TodoResponse>(TestJson.Options))!.IsDone);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await bob.PutAsJsonAsync($"/todos/{todo.Id}", new { title = "х", dueAt = "2027-01-01T00:00:00Z" })).StatusCode);
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
    public async Task Stats_overdue_uses_the_day_start_sent_by_the_client()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/todos", new { title = "срок 14 ноября", dueAt = "2026-11-14T10:00:00Z" });

        static string Url(string start) => $"/todos/stats?todayStart={Uri.EscapeDataString(start)}";
        var sameDay = await client.GetFromJsonAsync<TodoStatsResponse>(Url("2026-11-14T00:00:00Z"), TestJson.Options);
        var nextDay = await client.GetFromJsonAsync<TodoStatsResponse>(Url("2026-11-15T00:00:00Z"), TestJson.Options);

        Assert.Equal(0, sameDay!.Overdue);     // в день срока ещё не просрочено
        Assert.Equal(1, nextDay!.Overdue);     // на следующий день просрочено
    }
}
