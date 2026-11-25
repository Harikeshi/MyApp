using MyApp.Application.Todos;
using MyApp.Web.Models;

namespace MyApp.Web.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjects(this IEndpointRouteBuilder app)
    {
        var projects = app.MapGroup("/projects").RequireAuthorization();

        // проекты, где пользователь владелец или ответственный
        projects.MapGet("/", (ProjectService service, CancellationToken ct) => service.ListAsync(ct));

        projects.MapPost("/", async (ProjectRequest req, ProjectService service, CancellationToken ct) =>
            (await service.CreateAsync(req.Name, req.MemberIds ?? [], ct))
                .ToResult(project => Results.Created($"/projects/{project.Id}", project)));

        projects.MapPut("/{id:int}", async (int id, ProjectRequest req, ProjectService service, CancellationToken ct) =>
            (await service.UpdateAsync(id, req.Name, req.MemberIds ?? [], ct)).ToResult(project => Results.Ok(project)));

        projects.MapDelete("/{id:int}", async (int id, ProjectService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToResult(_ => Results.NoContent()));

        // поиск пользователей для списка ответственных: [{ id, name }], от 2 символов, не больше 10
        app.MapGet("/users", (string? search, ProjectService service, CancellationToken ct) =>
            service.SearchUsersAsync(search, ct))
            .RequireAuthorization();

        return app;
    }
}
