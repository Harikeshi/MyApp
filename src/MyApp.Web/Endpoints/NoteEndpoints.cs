using MyApp.Application.Notes;
using MyApp.Web.Models;

namespace MyApp.Web.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNotes(this IEndpointRouteBuilder app)
    {
        var notes = app.MapGroup("/notes").RequireAuthorization();

        // список без содержимого: [{ id, title, tag, updatedAt, preview }]
        notes.MapGet("/", (NoteService service, CancellationToken ct) => service.ListAsync(ct));

        notes.MapGet("/{id:int}", async (int id, NoteService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } note ? Results.Ok(note) : Results.NotFound());

        notes.MapPost("/", async (NoteRequest req, NoteService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(req.Title, req.Tag, req.Content, ct);
            return Results.Created($"/notes/{created.Id}", created);
        });

        notes.MapPut("/{id:int}", async (int id, NoteRequest req, NoteService service, CancellationToken ct) =>
            await service.UpdateAsync(id, req.Title, req.Tag, req.Content, ct) is { } note
                ? Results.Ok(note)
                : Results.NotFound());

        notes.MapDelete("/{id:int}", async (int id, NoteService service, CancellationToken ct) =>
            await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
