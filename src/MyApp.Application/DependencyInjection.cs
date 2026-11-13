using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Todos;

namespace MyApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TodoService>();
        return services;
    }
}