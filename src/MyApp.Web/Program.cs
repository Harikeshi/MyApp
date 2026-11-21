using MyApp.Application;
using MyApp.Infrastructure;
using MyApp.Web.Endpoints;
using MyApp.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Вложения в чатах идут через этот сервис (YARP) в AiChat.Api. Лимит Kestrel по умолчанию около 30 МБ:
// держим его не меньше лимита AiChat (Storage:MaxRequestBytes). Значение можно задать: Upload__MaxRequestBytes.
builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = builder.Configuration.GetValue("Upload:MaxRequestBytes", 45L * 1024 * 1024));

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
app.MapNews();
app.MapAiChatProxy();

app.Run();

// нужен интеграционным тестам (WebApplicationFactory<Program>)
public partial class Program { }
