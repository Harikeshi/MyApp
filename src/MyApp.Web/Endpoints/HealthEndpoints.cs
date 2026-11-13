using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace MyApp.Web.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        // процесс жив (в базу не ходит)
        app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });

        // приложение готово: база доступна, миграции применены
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });

        return app;
    }
}
