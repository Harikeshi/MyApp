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
    public async Task Create_saves_todo_for_current_user_with_utc_time()
    {
        var store = new FakeStore();

        var created = await ServiceFor("u1", store).CreateAsync("  Купить молоко ", null, Priority.High, default);

        Assert.Equal("Купить молоко", created.Title);
        Assert.Equal(Now.UtcDateTime, created.CreatedAt);
        Assert.Equal("u1", Assert.Single(store.Items).UserId);
        Assert.Equal(1, store.SaveCount);
    }

    [Fact]
    public async Task Update_of_foreign_todo_returns_false_and_saves_nothing()
    {
        var store = new FakeStore();
        store.Add(new TodoItem("u2", "чужая", null, Priority.Normal, Now.UtcDateTime));
        var foreignId = store.Items[0].Id;

        var updated = await ServiceFor("u1", store)
            .UpdateAsync(foreignId, "новое имя", true, null, Priority.Low, default);

        Assert.False(updated);
        Assert.Equal("чужая", store.Items[0].Title);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task Delete_removes_only_own_todo()
    {
        var store = new FakeStore();
        store.Add(new TodoItem("u1", "моя", null, Priority.Normal, Now.UtcDateTime));
        store.Add(new TodoItem("u2", "чужая", null, Priority.Normal, Now.UtcDateTime));
        var service = ServiceFor("u1", store);

        Assert.True(await service.DeleteAsync(store.Items[0].Id, default));
        Assert.False(await service.DeleteAsync(store.Items[0].Id, default));   // осталась только чужая

        Assert.Equal("u2", Assert.Single(store.Items).UserId);
    }

    [Fact]
    public async Task Stats_use_utc_today_unless_client_day_is_given()
    {
        var store = new FakeStore();
        var service = ServiceFor("u1", store);

        await service.GetStatsAsync(null, default);
        Assert.Equal(new DateOnly(2026, 11, 14), store.LastStatsDay);

        await service.GetStatsAsync(new DateOnly(2026, 11, 15), default);
        Assert.Equal(new DateOnly(2026, 11, 15), store.LastStatsDay);
    }

    [Fact]
    public async Task List_uses_default_paging_when_none_is_given()
    {
        var store = new FakeStore();

        await ServiceFor("u1", store).ListAsync(null, null, default);

        Assert.Equal(new PageRequest(1, PageRequest.DefaultPageSize), store.LastPage);
    }

    [Fact]
    public async Task List_clamps_invalid_paging()
    {
        var store = new FakeStore();

        await ServiceFor("u1", store).ListAsync(0, 1000, default);

        Assert.Equal(new PageRequest(1, PageRequest.MaxPageSize), store.LastPage);
    }

    [Fact]
    public async Task List_returns_requested_page_with_totals()
    {
        var store = new FakeStore();
        for (var i = 0; i < 25; i++)
            store.Add(new TodoItem("u1", $"задача {i}", null, Priority.Normal, Now.UtcDateTime));

        var page = await ServiceFor("u1", store).ListAsync(3, 10, default);

        Assert.Equal(5, page.Items.Count);       // 25 задач по 10: на третьей странице осталось 5
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    private static TodoService ServiceFor(string userId, FakeStore store) =>
        new(store, store, new FakeUser(userId), new FixedClock(Now));

    private sealed class FakeUser(string id) : ICurrentUser
    {
        public string Id { get; } = id;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : ITodoRepository, IUnitOfWork
    {
        private int _nextId;

        public List<TodoItem> Items { get; } = [];
        public int SaveCount { get; private set; }
        public DateOnly? LastStatsDay { get; private set; }
        public PageRequest LastPage { get; private set; }

        public Task<PagedResult<TodoItem>> ListAsync(string userId, PageRequest paging, CancellationToken ct)
        {
            LastPage = paging;
            var own = Items.Where(t => t.UserId == userId).ToList();
            var items = own.Skip(paging.Skip).Take(paging.PageSize).ToList();
            return Task.FromResult(new PagedResult<TodoItem>(items, paging.Page, paging.PageSize, own.Count));
        }

        public Task<TodoItem?> FindAsync(int id, string userId, CancellationToken ct) =>
            Task.FromResult<TodoItem?>(Items.FirstOrDefault(t => t.Id == id && t.UserId == userId));

        public void Add(TodoItem todo)
        {
            // Id выдаёт база; в заглушке выставляем его сами (сеттер закрыт)
            typeof(TodoItem).GetProperty(nameof(TodoItem.Id))!.SetValue(todo, ++_nextId);
            Items.Add(todo);
        }

        public void Remove(TodoItem todo) => Items.Remove(todo);

        public Task<TodoStatsResponse> GetStatsAsync(string userId, DateOnly today, CancellationToken ct)
        {
            LastStatsDay = today;
            return Task.FromResult(TodoStatsResponse.Empty);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
