using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MyApp.Infrastructure.Persistence;

/// <summary>Приложение считается неготовым, если в базе не применены миграции.</summary>
public class MigrationsHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        return pending.Any()
            ? HealthCheckResult.Unhealthy("Есть не применённые миграции")
            : HealthCheckResult.Healthy();
    }
}
