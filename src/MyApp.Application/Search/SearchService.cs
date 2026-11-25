using MyApp.Application.Abstractions;

namespace MyApp.Application.Search;

public class SearchService(ISearchRepository search, ICurrentUser user)
{
    public const int MinLength = 2;
    public const int MaxLength = 100;
    public const int PerGroup = 10;

    public Task<SearchResponse> SearchAsync(string? query, CancellationToken ct)
    {
        var q = query?.Trim() ?? "";
        if (q.Length < MinLength) return Task.FromResult(SearchResponse.Empty);
        if (q.Length > MaxLength) q = q[..MaxLength];

        return search.SearchAsync(user.Id, q, PerGroup, ct);
    }
}
