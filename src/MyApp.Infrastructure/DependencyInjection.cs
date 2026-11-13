using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Application.Abstractions;
using MyApp.Application.Accounts;
using MyApp.Infrastructure.Email;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
    {
        // ---------- база данных ----------
        // В тестах (среда Testing) базу подставляет ApiFactory.
        if (!env.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        }
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---------- Identity ----------
        services.AddIdentityApiEndpoints<ApplicationUser>(o =>
        {
            o.SignIn.RequireConfirmedEmail = true;
            o.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IAccountService, IdentityAccountService>();

        // ---------- почта ----------
        // Production по умолчанию Resend, остальные среды (Development, Testing) пишут письма в журнал.
        // Переключатель: Email__Provider = Resend | Logging
        services.AddHttpClient();

        var provider = cfg["Email:Provider"] ?? (env.IsProduction() ? "Resend" : "Logging");
        if (provider.Equals("Resend", StringComparison.OrdinalIgnoreCase))
        {
            // Без ключей приложение не запустится (и выкладка откатится), а не упадёт при первой регистрации.
            services.AddOptions<ResendOptions>()
                .BindConfiguration("Resend")
                .ValidateDataAnnotations()
                .ValidateOnStart();
            services.AddSingleton<IEmailGateway, ResendEmailGateway>();
        }
        else
        {
            services.AddSingleton<IEmailGateway, LoggingEmailGateway>();
        }

        // Регистрируется ПОСЛЕ AddIdentityApiEndpoints, чтобы заменить стандартный отправитель.
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

        // ---------- проверки состояния ----------
        var health = services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

        if (!env.IsEnvironment("Testing"))
            health.AddCheck<MigrationsHealthCheck>("migrations", tags: ["ready"]);

        return services;
    }
}