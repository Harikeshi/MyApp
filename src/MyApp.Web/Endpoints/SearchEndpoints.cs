using MyApp.Application.Search;

namespace MyApp.Web.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearch(this IEndpointRouteBuilder app)
    {
        // GET /search?q=текст -> { tasks, notes, projects }; от 2 символов, по 10 результатов в каждой группе
        app.MapGet("/search", (string? q, SearchService service, CancellationToken ct) => service.SearchAsync(q, ct))
            .RequireAuthorization();

        return app;
    }
}
