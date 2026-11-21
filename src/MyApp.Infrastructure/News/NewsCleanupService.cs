using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure.News;

/// <summary>Удаляет старые новости, под которыми нет комментариев.</summary>
public sealed class NewsCleanupService(
    IServiceScopeFactory scopes,
    IOptions<NewsOptions> options,
    ILogger<NewsCleanupService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.Enabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.RetentionDays);
                    var deleted = await db.News
                        .Where(n => n.PublishedAt < cutoff && !db.NewsComments.Any(c => c.NewsItemId == n.Id))
                        .ExecuteDeleteAsync(ct);

                    if (deleted > 0) log.LogInformation("Удалено старых новостей: {Count}", deleted);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Ошибка очистки старых новостей");
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
