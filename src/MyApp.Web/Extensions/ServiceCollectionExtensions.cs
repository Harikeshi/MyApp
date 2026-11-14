using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using MyApp.Application.Abstractions;
using MyApp.Web.Security;
using MyApp.Web.Services;
using Serilog;

namespace MyApp.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static WebApplicationBuilder AddAppLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console());

        return builder;
    }

    public static IServiceCollection AddWebDefaults(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddOpenApi();
        services.AddAuthorization();
        services.AddProblemDetails();          // единый формат ошибок (RFC 9457) для необработанных исключений и пустых 4xx/5xx

        // текущий пользователь для слоя Application
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // проверка запросов и JSON (приоритет словом, а не числом)
        services.AddValidation();
        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

        // ключи Data Protection (на сервере: DataProtection__KeysPath=/var/lib/myapp)
        var dataProtection = services.AddDataProtection().SetApplicationName("MyApp");
        var keysPath = cfg["DataProtection:KeysPath"];
        if (!string.IsNullOrEmpty(keysPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        // защита эндпоинтов авторизации (сама проверка captcha живёт в Infrastructure: ICaptchaVerifier)
        services.AddScoped<CaptchaFilter>();
        services.AddSingleton<RegistrationGate>();
        services.AddAppRateLimiting();

        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // лимит на IP; в тестах значение увеличивается настройкой RateLimit:AuthPerMinute
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

        return services;
    }
}
