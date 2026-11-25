using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Application.Abstractions;
using MyApp.Application.Accounts;
using MyApp.Infrastructure.Captcha;
using MyApp.Infrastructure.Email;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.News;
using MyApp.Infrastructure.Persistence;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg, IHostEnvironment env)
    {
        // ---------- база данных ----------
        // В тестах (среда Testing) базу подставляет ApiFactory.
        if (!env.IsEnvironment("Testing"))
        {
            services.AddDbContext<AppDbContext>(o =>
                o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
        }
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITodoRepository, TodoRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<INoteRepository, NoteRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<INewsRepository, NewsRepository>();

        // ---------- новости (опрос лент, очистка, проверка /health/news) ----------
        services.AddNewsFeed(env);

        // ---------- captcha ----------
        services.AddOptions<TurnstileOptions>().BindConfiguration(TurnstileOptions.SectionName);
        services.AddSingleton<ICaptchaVerifier, TurnstileCaptchaVerifier>();

        // ---------- Identity ----------
        services.AddIdentityApiEndpoints<ApplicationUser>(o =>
        {
            o.SignIn.RequireConfirmedEmail = true;
            o.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>();

        // Длинная сессия. Клиент обновляет токен доступа через /refresh, пока жив refresh-токен,
        // поэтому вход нужен раз в несколько недель, а не раз в час (по умолчанию Identity: 1 час и 14 дней).
        // Сервис AiChat читает те же токены и срок берёт из самого токена, ему менять ничего не нужно.
        services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, o =>
        {
            o.BearerTokenExpiration = TimeSpan.FromMinutes(cfg.GetValue("Auth:AccessTokenMinutes", 120));
            o.RefreshTokenExpiration = TimeSpan.FromDays(cfg.GetValue("Auth:RefreshTokenDays", 30));
        });

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