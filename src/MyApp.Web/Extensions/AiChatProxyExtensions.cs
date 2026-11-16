using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace MyApp.Web.Extensions;

/// <summary>
/// Чаты работают в отдельном сервисе (AiChat.Api). Браузер ходит на тот же адрес, что и за страницей,
/// а /api/** пересылается сервису: CORS и правки Nginx не нужны. Адрес сервиса: AiChat__BaseUrl.
/// </summary>
public static class AiChatProxyExtensions
{
    public static IServiceCollection AddAiChatProxy(this IServiceCollection services, IConfiguration cfg)
    {
        var address = cfg["AiChat:BaseUrl"] ?? "http://localhost:5058";

        services.AddReverseProxy().LoadFromMemory(
            routes:
            [
                new RouteConfig
                {
                    RouteId = "aichat",
                    ClusterId = "aichat",
                    AuthorizationPolicy = "default",          // без входа дальше не пускаем
                    Match = new RouteMatch { Path = "/api/{**catch-all}" }
                }
            ],
            clusters:
            [
                new ClusterConfig
                {
                    ClusterId = "aichat",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["main"] = new() { Address = address }
                    },
                    // Ответ модели приходит одним куском и может идти минуты:
                    // по умолчанию прокси оборвал бы запрос после 100 секунд ожидания.
                    HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromMinutes(10) }
                }
            ]);

        return services;
    }

    public static IEndpointRouteBuilder MapAiChatProxy(this IEndpointRouteBuilder app)
    {
        app.MapReverseProxy();
        return app;
    }
}
