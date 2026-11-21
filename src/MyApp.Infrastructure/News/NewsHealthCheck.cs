using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.News;

/// <summary>
/// Сообщает, если ни один источник не обновлялся дольше HealthMaxAgeMinutes.
/// Не входит в /health/ready: сбой новостей не должен делать приложение «неготовым». Адрес: /health/news.
/// </summary>
public sealed class NewsHealthCheck(AppDbContext db, IOptions<NewsOptions> options) : IHealthCheck
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var o = options.Value;
        if (!o.Enabled) return HealthCheckResult.Healthy("Опрос новостей выключен");

        var maxAge = TimeSpan.FromMinutes(o.HealthMaxAgeMinutes);
        if (!await db.NewsSources.AnyAsync(s => s.IsEnabled, ct))
            return HealthCheckResult.Healthy("Нет включённых источников");

        var newest = await db.NewsSources.Where(s => s.IsEnabled).MaxAsync(s => s.LastSuccessAt, ct);

        if (newest is null)
        {
            // Сразу после старта первого опроса ещё не было.
            return DateTimeOffset.UtcNow - StartedAt < maxAge
                ? HealthCheckResult.Healthy("Ожидание первого опроса")
                : HealthCheckResult.Unhealthy("Опрос новостей ни разу не прошёл успешно");
        }

        var age = DateTimeOffset.UtcNow - newest.Value;
        return age <= maxAge
            ? HealthCheckResult.Healthy($"Последний успешный опрос {age.TotalMinutes:F0} мин назад")
            : HealthCheckResult.Unhealthy($"Последний успешный опрос {age.TotalMinutes:F0} мин назад");
    }
}
