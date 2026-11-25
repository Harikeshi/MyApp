using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.News;
using MyApp.Application.Notes;
using MyApp.Application.Search;
using MyApp.Application.Todos;

namespace MyApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TodoService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<NoteService>();
        services.AddScoped<SearchService>();
        services.AddScoped<NewsService>();
        return services;
    }
}
