using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;

namespace MyApp.Web.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Конвейер обработки запросов. Порядок важен.</summary>
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        // настоящий IP и схема клиента за Nginx
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // старый встроенный /register закрыт: регистрация идёт через /account/register (с именем)
        app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Path.Equals("/register", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            // браузер всегда проверяет, не изменился ли файл (после выкладки не остаётся старого app.js)
            OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache"
        });

        app.UseSerilogRequestLogging(o => o.GetLevel = (ctx, _, ex) =>
            ex is OperationCanceledException && ctx.RequestAborted.IsCancellationRequested ? LogEventLevel.Debug
            : ctx.Request.Path.StartsWithSegments("/health") && ctx.Response.StatusCode < 500 ? LogEventLevel.Verbose
            : ex is not null || ctx.Response.StatusCode >= 500 ? LogEventLevel.Error
            : LogEventLevel.Information);

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        return app;
    }
}
