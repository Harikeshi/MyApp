using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MyApp.Infrastructure.News;

public static class NewsServiceCollectionExtensions
{
    /// <summary>Вызывается из AddInfrastructure. В среде Testing воркеры и проверка не регистрируются: тесты не ходят в интернет.</summary>
    public static IServiceCollection AddNewsFeed(this IServiceCollection services, IHostEnvironment env)
    {
        services.AddOptions<NewsOptions>()
            .BindConfiguration(NewsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // HTTP-клиент для опроса лент.
        services.AddHttpClient(NewsPollingService.HttpClientName, (sp, c) =>
            {
                var o = sp.GetRequiredService<IOptions<NewsOptions>>().Value;
                c.Timeout = TimeSpan.FromSeconds(20);
                c.MaxResponseContentBufferSize = 5 * 1024 * 1024;   // защита от огромных ответов
                c.DefaultRequestHeaders.UserAgent.ParseAdd(o.UserAgent);
                c.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/rss+xml, application/atom+xml, application/xml;q=0.9, text/xml;q=0.9, */*;q=0.8");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                MaxAutomaticRedirections = 5,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        if (env.IsEnvironment("Testing")) return services;

        // Порядок важен: сидер должен отработать до первого опроса.
        services.AddHostedService<NewsSeeder>();
        services.AddHostedService<NewsPollingService>();
        services.AddHostedService<NewsCleanupService>();

        services.AddHealthChecks().AddCheck<NewsHealthCheck>("news", tags: ["news"]);

        return services;
    }
}
