using MyApp.Application.Todos;
using MyApp.Web.Models;

namespace MyApp.Web.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodos(this IEndpointRouteBuilder app)
    {
        var todos = app.MapGroup("/todos").RequireAuthorization();

        // GET /todos?projectId=3&page=1&pageSize=20 -> { items, page, pageSize, totalCount, totalPages }
        // без projectId — все задачи, которые видит пользователь
        todos.MapGet("/", (int? projectId, int? page, int? pageSize, TodoService service, CancellationToken ct) =>
            service.ListAsync(projectId, page, pageSize, ct));

        todos.MapPost("/", async (CreateTodoRequest req, TodoService service, CancellationToken ct) =>
            (await service.CreateAsync(req.Title, req.DueAt, req.Priority, req.AssigneeId, req.ProjectId, ct))
                .ToResult(created => Results.Created($"/todos/{created.Id}", created)));

        // изменить текст, срок, приоритет, исполнителя (автор задачи или владелец проекта)
        todos.MapPut("/{id:int}", async (int id, UpdateTodoRequest req, TodoService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, req.Title, req.DueAt, req.Priority, req.AssigneeId, ct))
                .ToResult(todo => Results.Ok(todo)));

        // отметка «выполнено» (любой, кто видит задачу)
        todos.MapPatch("/{id:int}/done", async (int id, SetDoneRequest req, TodoService service, CancellationToken ct) =>
            (await service.SetDoneAsync(id, req.IsDone, ct)).ToResult(todo => Results.Ok(todo)));

        todos.MapDelete("/{id:int}", async (int id, TodoService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToResult(_ => Results.NoContent()));

        // todayStart — начало текущего дня клиента (ISO 8601, необязательно); всё, что раньше, «просрочено»
        todos.MapGet("/stats", (DateTimeOffset? todayStart, TodoService service, CancellationToken ct) =>
            service.GetStatsAsync(todayStart, ct));

        return app;
    }
}
