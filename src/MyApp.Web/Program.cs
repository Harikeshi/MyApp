using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks; // Для проверки /health     
using MyApp.Web.Data;
using MyApp.Web.Models;
using MyApp.Web.Services;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

/* logging */
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddOpenApi();

/* настройка JSON, для отображения high */
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

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
builder.Services.AddIdentityApiEndpoints<ApplicationUser>(o =>
{
    o.SignIn.RequireConfirmedEmail = true;
    o.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AppDbContext>();

// Время жизни токена, но вход придется делать каждые полчаса, пока не будет /refresh
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme,
    o => o.BearerTokenExpiration = TimeSpan.FromMinutes(30));

var emailProvider = builder.Configuration["Email:Provider"]
    ?? (builder.Environment.IsDevelopment() ? "Logging" : "SendGrid");

switch (emailProvider)
{
    case "Resend":
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, ResendEmailSender>();
        break;
    default:
        builder.Services.AddSingleton<IEmailSender<ApplicationUser>, LoggingEmailSender>();
        break;
}

/* Проверка базы */
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

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

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseDefaultFiles();      // отдаёт wwwroot/index.html на "/"
app.UseStaticFiles();
//app.UseSerilogRequestLogging();
/* Условие включает настройки, чтобы cancelationtoken не попадали в ошибки */
app.UseSerilogRequestLogging(o => o.GetLevel = (ctx, _, ex) =>
    ex is OperationCanceledException && ctx.RequestAborted.IsCancellationRequested ? LogEventLevel.Debug
    : ctx.Request.Path.StartsWithSegments("/health") && ctx.Response.StatusCode < 500 ? LogEventLevel.Verbose
    : ex is not null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
    : LogEventLevel.Information);

app.UseRateLimiter();
// после UseRateLimiter
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });          // «процесс отвечает»
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });  // + база данных

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

/* endpoints */
app.MapIdentityApi<ApplicationUser>().RequireRateLimiting("auth").AddEndpointFilter(async (context, next) =>
{
    // Здесь вызовите ваш CaptchaFilter или реализуйте логику проверки капчи
    // Например, получите экземпляр фильтра через context.HttpContext.RequestServices
    // и вызовите его метод InvokeAsync
    var captchaFilter = context.HttpContext.RequestServices.GetRequiredService<CaptchaFilter>();
    return await captchaFilter.InvokeAsync(context, next);
});

app.MapPost("/account/forgot", async (
        ForgotRequest req,
        UserManager<ApplicationUser> users,
        IEmailSender<ApplicationUser> sender) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null)
        return Results.NotFound(new { error = "email_not_registered" });
    if (!await users.IsEmailConfirmedAsync(user))
        return Results.BadRequest(new { error = "email_not_confirmed" });

    var token = await users.GeneratePasswordResetTokenAsync(user);
    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    await sender.SendPasswordResetCodeAsync(user, req.Email, HtmlEncoder.Default.Encode(code));
    return Results.Ok();
})
.RequireRateLimiting("auth")
.AddEndpointFilter<CaptchaFilter>();

app.MapPost("/account/register", async (
        RegisterRequest req,
        UserManager<ApplicationUser> users,
        IEmailSender<ApplicationUser> sender,
        IConfiguration cfg) =>
{
    var name = req.DisplayName.Trim();
    if (name.Length < 2)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        { ["DisplayName"] = ["Имя должно содержать от 2 до 30 символов"] });

    var user = new ApplicationUser { UserName = req.Email, Email = req.Email, DisplayName = name };
    var result = await users.CreateAsync(user, req.Password);
    if (!result.Succeeded)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var e in result.Errors)
            errors[e.Code] = errors.TryGetValue(e.Code, out var old) ? [.. old, e.Description] : [e.Description];
        return Results.ValidationProblem(errors);
    }

    var token = await users.GenerateEmailConfirmationTokenAsync(user);
    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    var baseUrl = (cfg["App:BaseUrl"] ?? "http://localhost:5247").TrimEnd('/');
    var link = $"{baseUrl}/confirmEmail?userId={Uri.EscapeDataString(user.Id)}&code={Uri.EscapeDataString(code)}";
    await sender.SendConfirmationLinkAsync(user, req.Email, HtmlEncoder.Default.Encode(link));

    return Results.Ok();
})
.RequireRateLimiting("auth")
.AddEndpointFilter<CaptchaFilter>();
//using System.ComponentModel.DataAnnotations; System.Text; System.Text.Encodings.Web; Microsoft.AspNetCore.WebUtilities;
app.MapPost("/logout", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
{
    var user = await users.GetUserAsync(principal);
    if (user is not null)
        await users.UpdateSecurityStampAsync(user);   // refresh-токены перестают работать
    return Results.NoContent();
}).RequireAuthorization();

// Возвращает имя и почту
app.MapGet("/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
{
    var user = await users.GetUserAsync(principal);
    if (user is null) return Results.Unauthorized();
    var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName;
    return Results.Ok(new { name, email = user.Email });
}).RequireAuthorization();
var todos = app.MapGroup("/todos").RequireAuthorization();

static string UserId(ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier)!;

todos.MapGet("/", async (AppDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
{
    var uid = UserId(user);
    return await db.Todos
        .Where(t => t.UserId == uid)
        .OrderBy(t => t.IsDone)
        .ThenByDescending(t => t.Priority)
        .ThenBy(t => t.DueDate == null)      // задачи без срока в конец
        .ThenBy(t => t.DueDate)
        .ThenBy(t => t.Id)
        .Select(t => new TodoResponse(t.Id, t.Title, t.IsDone, t.CreatedAt, t.DueDate, t.Priority))
        .ToListAsync(ct);
});

todos.MapPost("/", async (AppDbContext db, ClaimsPrincipal user, CreateTodoRequest req, CancellationToken ct) =>
{
    var todo = new TodoItem { Title = req.Title.Trim(), UserId = UserId(user), DueDate = req.DueDate, Priority = req.Priority };
    //new TodoItem { Title = req.Title.Trim(), UserId = UserId(user) };
    db.Todos.Add(todo);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/todos/{todo.Id}",
        new TodoResponse(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt, todo.DueDate, todo.Priority));
        //new TodoResponse(todo.Id, todo.Title, todo.IsDone, todo.CreatedAt));
});

todos.MapPut("/{id}", async (AppDbContext db, ClaimsPrincipal user, int id, UpdateTodoRequest req, CancellationToken ct) =>
{
    var uid = UserId(user);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid, ct);
    if (todo is null) return Results.NotFound();
    todo.Title = req.Title.Trim();
    todo.IsDone = req.IsDone;
    todo.DueDate = req.DueDate;
    todo.Priority = req.Priority;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

todos.MapDelete("/{id}", async (AppDbContext db, ClaimsPrincipal user, int id, CancellationToken ct) =>
{
    var uid = UserId(user);
    var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == uid, ct);
    if (todo is null) return Results.NotFound();
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public record ForgotRequest(string Email);

public partial class Program { }