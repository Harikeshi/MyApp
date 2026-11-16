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

app.UseAppPipeline();    // порядок middleware (в том числе статические файлы) описан там

app.MapHealth();
app.MapAccounts();
app.MapTodos();
app.MapAiChatProxy();

app.Run();

// нужен интеграционным тестам (WebApplicationFactory<Program>)
public partial class Program { }
