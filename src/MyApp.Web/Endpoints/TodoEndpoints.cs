using MyApp.Application.Todos;
using MyApp.Web.Models;

namespace MyApp.Web.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodos(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/todos").RequireAuthorization();

        todos.MapGet("/", (TodoService service, CancellationToken ct) =>
            service.ListAsync(ct));

        todos.MapPost("/", async (CreateTodoRequest req, TodoService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(req.Title, req.DueDate, req.Priority, ct);
            return Results.Created($"/todos/{created.Id}", created);
        });

        todos.MapPut("/{id:int}", async (int id, UpdateTodoRequest req, TodoService service, CancellationToken ct) =>
            await service.UpdateAsync(id, req.Title, req.IsDone, req.DueDate, req.Priority, ct)
                ? Results.NoContent()
                : Results.NotFound());

        todos.MapDelete("/{id:int}", async (int id, TodoService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct)
                ? Results.NoContent()
                : Results.NotFound());

        todos.MapGet("/stats", (TodoService service, CancellationToken ct) => service.GetStatsAsync(ct));
        
        return app;
    }
}
