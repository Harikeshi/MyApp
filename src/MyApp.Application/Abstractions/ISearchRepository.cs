using MyApp.Application.Search;

namespace MyApp.Application.Abstractions;

public interface ISearchRepository
{
    /// <summary>Поиск по задачам, проектам и записям пользователя (регистр не важен).</summary>
    Task<SearchResponse> SearchAsync(string userId, string query, int take, CancellationToken ct);
}
