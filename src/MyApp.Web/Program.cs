using Microsoft.EntityFrameworkCore;
using MyApp.Web.Data;
using MyApp.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddValidation();   // рядом с остальными builder.Services...

var app = builder.Build();
app.MapOpenApi();

app.MapGet("/todos", async (AppDbContext db) =>
    await db.Todos.OrderBy(t => t.Id)
        .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt))
        .ToListAsync());

app.MapPost("/todos", async (AppDbContext db, CreateTodoRequest req) =>
{
    var todo = new TodoItem { Title = req.Title.Trim() };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}",
        new TodoResponse(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt));
});

app.MapPut("/todos/{id}", async (AppDbContext db, int id, UpdateTodoRequest req) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    todo.Title = req.Title.Trim();
    todo.IsDone = req.IsDone;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/todos/{id}", async (AppDbContext db, int id) =>
{
var todo = await db.Todos.FindAsync(id);
if (todo is null) return Results.NotFound();
db.Todos.Remove(todo);
await db.SaveChangesAsync();
return Results.NoContent();
});



app.Run();
