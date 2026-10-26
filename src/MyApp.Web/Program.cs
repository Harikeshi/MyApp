using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Web.Data;
using MyApp.Web.Models;
using MyApp.Web.Services;
using Serilog;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

/* logging */
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddOpenApi();

/* для паролей */
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("MyApp");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

/* db */
if (!builder.Environment.IsEnvironment("Testing")) // Вне тестов 
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

/* validation */
builder.Services.AddValidation();

/* auth */
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>(o =>
{
    o.SignIn.RequireConfirmedEmail = true;
    o.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>();

// регистрируем ПОСЛЕ AddIdentityApiEndpoints, чтобы наша реализация победила
//builder.Services.AddSingleton<IEmailSender<IdentityUser>, LoggingEmailSender>();
//if (builder.Environment.IsDevelopment())
//    builder.Services.AddSingleton<IEmailSender<IdentityUser>, LoggingEmailSender>();
//else
//    builder.Services.AddSingleton<IEmailSender<IdentityUser>, SendGridEmailSender>();
//    //builder.Services.AddSingleton<IEmailSender<IdentityUser>, BrevoEmailSender>();
////builder.Services.AddSingleton<IEmailSender<IdentityUser>, SmtpEmailSender>(); SendGridEmailSender

var emailProvider = builder.Configuration["Email:Provider"]
    ?? (builder.Environment.IsDevelopment() ? "Logging" : "SendGrid");

switch (emailProvider)
{
    case "Resend":
        builder.Services.AddSingleton<IEmailSender<IdentityUser>, ResendEmailSender>();
        break;
    case "SendGrid":
        builder.Services.AddSingleton<IEmailSender<IdentityUser>, SendGridEmailSender>();
        break;
    default:
        builder.Services.AddSingleton<IEmailSender<IdentityUser>, LoggingEmailSender>();
        break;
}


builder.Services.AddHttpClient();
builder.Services.AddScoped<CaptchaFilter>();

/* rate limiting */
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.AddPolicy("auth", http =>
    {
        var limit = http.RequestServices.GetRequiredService<IConfiguration>()
            .GetValue("RateLimit:AuthPerMinute", 10);
        return RateLimitPartition.GetFixedWindowLimiter(
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    o.OnRejected = (ctx, ct) =>
    {
        var log = ctx.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>().CreateLogger("RateLimit");
        log.LogWarning("Превышен лимит запросов {Path} с {Ip}",
            ctx.HttpContext.Request.Path, ctx.HttpContext.Connection.RemoteIpAddress);
        return ValueTask.CompletedTask;
    };
});

var app = builder.Build();

if (app.Environment.IsProduction() && string.IsNullOrEmpty(app.Configuration["Turnstile:SecretKey"]))
    app.Logger.LogWarning("Turnstile:SecretKey не задан: captcha отключена"); // сразу после var app = builder.Build();

/* middleware: порядок важен */
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseDefaultFiles();      // отдаёт wwwroot/index.html на "/"
app.UseStaticFiles();
app.UseSerilogRequestLogging();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

/* endpoints */
//app.MapIdentityApi<IdentityUser>().RequireRateLimiting("auth");
app.MapIdentityApi<IdentityUser>().RequireRateLimiting("auth").AddEndpointFilter(async (context, next) =>
{
    // Здесь вызовите ваш CaptchaFilter или реализуйте логику проверки капчи
    // Например, получите экземпляр фильтра через context.HttpContext.RequestServices
    // и вызовите его метод InvokeAsync
    var captchaFilter = context.HttpContext.RequestServices.GetRequiredService<CaptchaFilter>();
    return await captchaFilter.InvokeAsync(context, next);
});

// todo: indentify 
// app.MapIdentityApi<IdentityUser>().RequireRateLimiting("auth").AddEndpointFilter<CaptchaFilter>(); // позже, с фронтендом

var todos = app.MapGroup("/todos").RequireAuthorization();

//todos.MapGet("/", async (AppDbContext db) =>
//    await db.Todos.OrderBy(t => t.Id)
//        .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt))
//        .ToListAsync());

//using System.Security.Claims;

static string UserId(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier)!;

todos.MapGet("/", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var uid = UserId(user);
    return await db.Todos
        .Where(t => t.UserId == uid)
        .OrderBy(t => t.Id)
        .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt))
        .ToListAsync();
});

todos.MapPost("/", async (AppDbContext db, ClaimsPrincipal user, CreateTodoRequest req) =>
{
    var todo = new TodoItem { Title = req.Title.Trim(), UserId = UserId(user) };
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}",
        new TodoResponse(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt));
});

todos.MapPut("/{id}", async (AppDbContext db, ClaimsPrincipal user, int id, UpdateTodoRequest req) =>
{
    var uid = UserId(user);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);
    if (todo is null) return Results.NotFound();
    todo.Title = req.Title.Trim();
    todo.IsDone = req.IsDone;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

todos.MapDelete("/{id}", async (AppDbContext db, ClaimsPrincipal user, int id) =>
{
    var uid = UserId(user);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }