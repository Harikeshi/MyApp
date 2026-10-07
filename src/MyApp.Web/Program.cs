using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Web.Data;
using MyApp.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

/* db */
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

/* validation*/
builder.Services.AddValidation();   // рядом с остальными builder.Services...

/* auth */
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();
app.MapOpenApi();

/* endpoints */

app.MapIdentityApi<IdentityUser>();     // /register, /login, /refresh и др.

var todos = app.MapGroup("/todos").RequireAuthorization();

app.MapGet("/", async (AppDbContext db) =>
    await db.Todos.OrderBy(t => t.Id)
        .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt))
        .ToListAsync());

app.MapPost("/", async (AppDbContext db, CreateTodoRequest req) =>
{
    var todo = new TodoItem { Title = req.Title.Trim() };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}",
        new TodoResponse(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt));
});

app.MapPut("/{id}", async (AppDbContext db, int id, UpdateTodoRequest req) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();
    todo.Title = req.Title.Trim();
    todo.IsDone = req.IsDone;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/{id}", async (AppDbContext db, int id) =>
{
var todo = await db.Todos.FindAsync(id);
if (todo is null) return Results.NotFound();
db.Todos.Remove(todo);
await db.SaveChangesAsync();
return Results.NoContent();
});



app.Run();
