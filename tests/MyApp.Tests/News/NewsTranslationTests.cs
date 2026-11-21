using MyApp.Application.Abstractions;
using MyApp.Application.News;
using MyApp.Domain.News;
using Xunit;

namespace MyApp.Tests.News;

public class NewsTranslationTests
{
    private sealed class FakeRepo : INewsRepository
    {
        public NewsItem? Item = new() { Id = 1, Title = "Bitcoin rises", Summary = "Details" };
        public TranslationResponse? Cached;
        public int CountToday;
        public NewsTranslation? Saved;

        public Task<NewsItem?> FindAsync(long newsId, CancellationToken ct) => Task.FromResult(Item);
        public Task<TranslationResponse?> GetTranslationAsync(long newsId, string lang, CancellationToken ct) => Task.FromResult(Cached);
        public Task<int> CountTranslationsSinceAsync(DateTimeOffset since, CancellationToken ct) => Task.FromResult(CountToday);
        public Task<TranslationResponse> SaveTranslationAsync(NewsTranslation t, CancellationToken ct)
        {
            Saved = t;
            return Task.FromResult(new TranslationResponse(t.Title, t.Summary));
        }

        public Task<NewsPageResponse> ListAsync(NewsTopic? topic, NewsCursor? cursor, int limit, string lang, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(long newsId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<CommentResponse>> ListCommentsAsync(long newsId, string currentUserId, CancellationToken ct) => throw new NotImplementedException();
        public Task<CommentResponse?> GetCommentAsync(long commentId, string currentUserId, CancellationToken ct) => throw new NotImplementedException();
        public void AddComment(NewsComment comment) => throw new NotImplementedException();
        public Task<bool> DeleteCommentAsync(long commentId, string userId, CancellationToken ct) => throw new NotImplementedException();
        public Task<ReportOutcome> ReportCommentAsync(long commentId, string userId, int hideAfter, CancellationToken ct) => throw new NotImplementedException();
    }

    private sealed class FakeTranslator : INewsTranslator
    {
        public bool IsConfigured { get; set; } = true;
        public string TargetLang => "ru";
        public string Model => "test-model";
        public int DailyLimit { get; set; } = 10;
        public bool Fail;
        public int Calls;

        public Task<(string Title, string? Summary)> TranslateAsync(string title, string? summary, CancellationToken ct)
        {
            Calls++;
            if (Fail) throw new TranslationFailedException("boom");
            return Task.FromResult(("Биткоин растёт", (string?)"Подробности"));
        }
    }

    private sealed class FakeUser : ICurrentUser { public string Id => "u1"; }
    private sealed class FakeUow : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0); }

    private static NewsService Create(FakeRepo repo, FakeTranslator tr) =>
        new(repo, tr, new FakeUow(), new FakeUser(), TimeProvider.System);

    [Fact]
    public async Task Cached_translation_is_returned_without_calling_the_model()
    {
        var repo = new FakeRepo { Cached = new TranslationResponse("Готово", null) };
        var tr = new FakeTranslator();

        var result = await Create(repo, tr).TranslateAsync(1, default);

        Assert.Equal(TranslationStatus.Ok, result.Status);
        Assert.Equal("Готово", result.Translation!.Title);
        Assert.Equal(0, tr.Calls);
    }

    [Fact]
    public async Task New_translation_is_created_and_saved()
    {
        var repo = new FakeRepo();
        var tr = new FakeTranslator();

        var result = await Create(repo, tr).TranslateAsync(1, default);

        Assert.Equal(TranslationStatus.Ok, result.Status);
        Assert.Equal(1, tr.Calls);
        Assert.Equal("ru", repo.Saved!.Lang);
        Assert.Equal("test-model", repo.Saved.Model);
        Assert.Equal(1, repo.Saved.NewsItemId);
    }

    [Fact]
    public async Task Missing_news_gives_not_found()
    {
        var result = await Create(new FakeRepo { Item = null }, new FakeTranslator()).TranslateAsync(1, default);
        Assert.Equal(TranslationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Not_configured_gives_unavailable_and_does_not_call_the_model()
    {
        var tr = new FakeTranslator { IsConfigured = false };
        var result = await Create(new FakeRepo(), tr).TranslateAsync(1, default);

        Assert.Equal(TranslationStatus.Unavailable, result.Status);
        Assert.Equal(0, tr.Calls);
    }

    [Fact]
    public async Task Daily_limit_stops_new_translations()
    {
        var tr = new FakeTranslator { DailyLimit = 5 };
        var result = await Create(new FakeRepo { CountToday = 5 }, tr).TranslateAsync(1, default);

        Assert.Equal(TranslationStatus.LimitReached, result.Status);
        Assert.Equal(0, tr.Calls);
    }

    [Fact]
    public async Task Translator_failure_gives_unavailable_and_saves_nothing()
    {
        var repo = new FakeRepo();
        var result = await Create(repo, new FakeTranslator { Fail = true }).TranslateAsync(1, default);

        Assert.Equal(TranslationStatus.Unavailable, result.Status);
        Assert.Null(repo.Saved);
    }
}
