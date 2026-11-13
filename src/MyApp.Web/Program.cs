using MyApp.Application;
using MyApp.Infrastructure;
using MyApp.Web.Endpoints;
using MyApp.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppLogging();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddWebDefaults(builder.Configuration);

var app = builder.Build();

app.UseDefaultFiles();   // отдаёт index.html по /
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // имена app.js/app.css не меняются между сборками, поэтому без этого браузер может держать старую версию
        if (ctx.Context.Request.Path.StartsWithSegments("/app"))
            ctx.Context.Response.Headers.CacheControl = "no-cache";
    }
}); 

app.UseAppPipeline();

app.MapHealth();
app.MapAccounts();
app.MapTodos();

app.Run();

// нужен интеграционным тестам (WebApplicationFactory<Program>)
public partial class Program { }
