using MyApp.Application.Abstractions;
using MyApp.Application.Common;
using MyApp.Application.Todos;
using MyApp.Domain.Todos;

namespace MyApp.Tests.Application;

/// <summary>Сервис проверяется без EF Core и HTTP: хранилище подменено простой заглушкой.</summary>
public class TodoServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 23, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_saves_todo_for_current_user_and_due_defaults_to_now()
    {
        var store = new FakeStore();

        var result = await ServiceFor("u1", store)
            .CreateAsync("  Купить молоко ", null, Priority.High, null, null, default);

        Assert.True(result.IsOk);
        var created = result.Value!;
        Assert.Equal("Купить молоко", created.Title);
        Assert.Equal(Now.UtcDateTime, created.CreatedAt);
        Assert.Equal(Now.UtcDateTime, created.DueAt);
        Assert.True(created.CanManage);
        Assert.Equal("u1", Assert.Single(store.Items).UserId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Create_keeps_the_given_due_time()
    {
        var store = new FakeStore();
        var due = new DateTime(2027, 1, 5, 9, 15, 0, DateTimeKind.Utc);

        var result = await ServiceFor("u1", store).CreateAsync("a", due, Priority.Normal, null, null, default);

        Assert.Equal(due, result.Value!.DueAt);
    }

    [Fact]
    public async Task Create_cannot_assign_a_stranger_without_a_project()
    {
        var store = new FakeStore();

        var result = await ServiceFor("u1", store).CreateAsync("a", null, Priority.Normal, "u2", null, default);

        Assert.Equal(OutcomeStatus.Invalid, result.Status);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Create_can_assign_oneself_or_a_project_member()
    {
        var store = new FakeStore();
        var projectId = store.AddProject("u1", members: ["u2"]);
        var service = ServiceFor("u1", store);

        Assert.True((await service.CreateAsync("a", null, Priority.Normal, "u1", null, default)).IsOk);
        var inProject = await service.CreateAsync("b", null, Priority.Normal, "u2", projectId, default);

        Assert.True(inProject.IsOk);
        Assert.Equal(projectId, inProject.Value!.ProjectId);
        Assert.Equal("u2", inProject.Value.AssigneeId);
        Assert.Equal(OutcomeStatus.Invalid,
            (await service.CreateAsync("c", null, Priority.Normal, "u3", projectId, default)).Status);
    }

    [Fact]
    public async Task Create_in_a_project_the_user_does_not_see_is_not_found()
    {
        var store = new FakeStore();
        var projectId = store.AddProject("u2", members: []);

        var result = await ServiceFor("u1", store).CreateAsync("a", null, Priority.Normal, null, projectId, default);

        Assert.Equal(OutcomeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Update_of_invisible_todo_is_not_found_and_saves_nothing()
    {
        var store = new FakeStore();
        store.Add(NewTodo("u2", "чужая"));
        var foreignId = store.Items[0].Id;

        var result = await ServiceFor("u1", store).UpdateAsync(foreignId, "новое", null, Priority.Low, null, default);

        Assert.Equal(OutcomeStatus.NotFound, result.Status);
        Assert.Equal("чужая", store.Items[0].Title);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Assignee_can_mark_done_but_cannot_edit_or_delete()
    {
        var store = new FakeStore();
        store.Add(NewTodo("u1", "задача", assignee: "u2"));
        var id = store.Items[0].Id;
        var service = ServiceFor("u2", store);

        Assert.True((await service.SetDoneAsync(id, true, default)).IsOk);
        Assert.Equal(OutcomeStatus.Forbidden, (await service.UpdateAsync(id, "x", null, Priority.Normal, null, default)).Status);
        Assert.Equal(OutcomeStatus.Forbidden, (await service.DeleteAsync(id, default)).Status);

        Assert.True(store.Items[0].IsDone);
        Assert.Equal("задача", store.Items[0].Title);
    }

    [Fact]
    public async Task Project_owner_can_edit_and_delete_a_members_todo()
    {
        var store = new FakeStore();
        var projectId = store.AddProject("u1", members: ["u2"]);
        store.Add(NewTodo("u2", "от участника", projectId: projectId));
        var id = store.Items[0].Id;
        var service = ServiceFor("u1", store);

        var updated = await service.UpdateAsync(id, "правка", null, Priority.High, "u2", default);
        Assert.True(updated.IsOk);
        Assert.Equal("правка", updated.Value!.Title);
        Assert.Equal("u2", updated.Value.AssigneeId);

        Assert.True((await service.DeleteAsync(id, default)).IsOk);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Update_changes_text_due_priority_and_assignee()
    {
        var store = new FakeStore();
        store.Add(NewTodo("u1", "a"));
        var id = store.Items[0].Id;
        var due = new DateTime(2027, 2, 1, 8, 0, 0, DateTimeKind.Utc);

        var result = await ServiceFor("u1", store).UpdateAsync(id, "b", due, Priority.High, "u1", default);

        Assert.True(result.IsOk);
        var todo = store.Items[0];
        Assert.Equal("b", todo.Title);
        Assert.Equal(due, todo.DueAt);
        Assert.Equal(Priority.High, todo.Priority);
        Assert.Equal("u1", todo.AssigneeId);
    }

    [Fact]
    public async Task Stats_use_utc_start_of_today_unless_client_day_is_given()
    {
        var store = new FakeStore();
        var service = ServiceFor("u1", store);

        await service.GetStatsAsync(null, default);
        Assert.Equal(new DateTime(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc), store.LastStatsStart);

        var clientStart = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.FromHours(3));
        await service.GetStatsAsync(clientStart, default);
        Assert.Equal(new DateTime(2026, 11, 13, 21, 0, 0, DateTimeKind.Utc), store.LastStatsStart);
    }

    [Fact]
    public async Task List_uses_default_paging_when_none_is_given()
    {
        var store = new FakeStore();

        await ServiceFor("u1", store).ListAsync(null, null, null, default);

        Assert.Equal(new PageRequest(1, PageRequest.DefaultPageSize), store.LastPage);
    }

    [Fact]
    public async Task List_clamps_invalid_paging()
    {
        var store = new FakeStore();

        await ServiceFor("u1", store).ListAsync(null, 0, 1000, default);

        Assert.Equal(new PageRequest(1, PageRequest.MaxPageSize), store.LastPage);
    }

    [Fact]
    public async Task List_returns_requested_page_with_totals()
    {
        var store = new FakeStore();
        for (var i = 0; i < 25; i++)
            store.Add(NewTodo("u1", $"задача {i}"));

        var page = await ServiceFor("u1", store).ListAsync(null, 3, 10, default);

        Assert.Equal(5, page.Items.Count);       // 25 задач по 10: на третьей странице осталось 5
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task List_filters_by_project_and_includes_assigned_and_project_todos()
    {
        var store = new FakeStore();
        var projectId = store.AddProject("u2", members: ["u1"]);
        store.Add(NewTodo("u1", "моя без проекта"));
        store.Add(NewTodo("u2", "в проекте", projectId: projectId));
        store.Add(NewTodo("u2", "мне назначена", assignee: "u1"));
        store.Add(NewTodo("u2", "чужая"));
        var service = ServiceFor("u1", store);

        var all = await service.ListAsync(null, null, null, default);
        var inProject = await service.ListAsync(projectId, null, null, default);

        Assert.Equal(3, all.TotalCount);
        Assert.Equal("в проекте", Assert.Single(inProject.Items).Title);
        Assert.False(inProject.Items[0].CanManage);        // участник не владелец и не автор
    }

    private static TodoItem NewTodo(string owner, string title, string? assignee = null, int? projectId = null) =>
        new(owner, title, Now.UtcDateTime, Priority.Normal, Now.UtcDateTime, assignee, projectId);

    private static TodoService ServiceFor(string userId, FakeStore store) =>
        new(store, store, store, store, new FakeUser(userId), new FixedClock(Now));

    private sealed class FakeUser(string id) : ICurrentUser
    {
        public string Id { get; } = id;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : ITodoRepository, IProjectRepository, IUserDirectory, IUnitOfWork
    {
        private int _nextId;
        private int _nextProjectId;

        public List<TodoItem> Items { get; } = [];
        public List<Project> Projects { get; } = [];
        public int SaveCount { get; private set; }
        public DateTime? LastStatsStart { get; private set; }
        public PageRequest LastPage { get; private set; }

        public int AddProject(string owner, string[] members)
        {
            var project = new Project(owner, "проект", Now.UtcDateTime);
            project.SetMembers(members);
            typeof(Project).GetProperty(nameof(Project.Id))!.SetValue(project, ++_nextProjectId);
            Projects.Add(project);
            return project.Id;
        }

        private bool Visible(TodoItem t, string userId) =>
            t.UserId == userId
            || t.AssigneeId == userId
            || (t.ProjectId is int pid && Projects.Any(p => p.Id == pid && p.IsParticipant(userId)));

        private TodoEntry Entry(TodoItem t) =>
            new(t, t.ProjectId is int pid ? Projects.FirstOrDefault(p => p.Id == pid)?.OwnerId : null);

        // ---- ITodoRepository ----
        public Task<PagedResult<TodoEntry>> ListAsync(string userId, int? projectId, PageRequest paging, CancellationToken ct)
        {
            LastPage = paging;
            var visible = Items.Where(t => Visible(t, userId) && (projectId is null || t.ProjectId == projectId)).ToList();
            var items = visible.Skip(paging.Skip).Take(paging.PageSize).Select(Entry).ToList();
            return Task.FromResult(new PagedResult<TodoEntry>(items, paging.Page, paging.PageSize, visible.Count));
        }

        public Task<TodoEntry?> FindAsync(int id, string userId, CancellationToken ct) =>
            Task.FromResult(Items.Where(t => t.Id == id && Visible(t, userId)).Select(Entry).FirstOrDefault());

        public void Add(TodoItem todo)
        {
            // Id выдаёт база; в заглушке выставляем его сами (сеттер закрыт)
            typeof(TodoItem).GetProperty(nameof(TodoItem.Id))!.SetValue(todo, ++_nextId);
            Items.Add(todo);
        }

        public void Remove(TodoItem todo) => Items.Remove(todo);

        public Task<TodoStatsResponse> GetStatsAsync(string userId, DateTime todayStartUtc, CancellationToken ct)
        {
            LastStatsStart = todayStartUtc;
            return Task.FromResult(TodoStatsResponse.Empty);
        }

        // ---- IProjectRepository ----
        public Task<IReadOnlyList<Project>> ListVisibleAsync(string userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Project>>(Projects.Where(p => p.IsParticipant(userId)).ToList());

        public Task<Project?> FindVisibleAsync(int id, string userId, CancellationToken ct) =>
            Task.FromResult(Projects.FirstOrDefault(p => p.Id == id && p.IsParticipant(userId)));

        public void Add(Project project) => Projects.Add(project);

        public void Remove(Project project) => Projects.Remove(project);

        // ---- IUserDirectory ----
        public Task<IReadOnlyDictionary<string, string>> GetNamesAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<string, string>>(ids.ToDictionary(id => id, id => $"имя {id}"));

        public Task<IReadOnlyList<UserInfo>> SearchAsync(string query, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<UserInfo>>([]);

        public Task<IReadOnlySet<string>> ExistingIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlySet<string>>(ids.ToHashSet());

        // ---- IUnitOfWork ----
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
