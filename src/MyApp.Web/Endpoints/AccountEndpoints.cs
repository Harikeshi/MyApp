using MyApp.Application.Accounts;
using MyApp.Infrastructure.Identity;
using MyApp.Web.Models;
using MyApp.Web.Security;

namespace MyApp.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccounts(this IEndpointRouteBuilder app)
    {
        // состояние для страницы (открыта ли регистрация)
        app.MapGet("/config", (RegistrationGate gate) => new { registrationEnabled = gate.IsOpen });

        app.MapIdentityApi<ApplicationUser>().RequireRateLimiting("auth").AddEndpointFilter(async (context, next) =>
        {
            // Здесь вызовите ваш CaptchaFilter или реализуйте логику проверки капчи
            // Например, получите экземпляр фильтра через context.HttpContext.RequestServices
            // и вызовите его метод InvokeAsync
            var captchaFilter = context.HttpContext.RequestServices.GetRequiredService<CaptchaFilter>();
            return await captchaFilter.InvokeAsync(context, next);
        });
        
        // // встроенные эндпоинты Identity: /login, /refresh, /confirmEmail, /resetPassword, ...
        // app.MapIdentityApi<ApplicationUser>().RequireRateLimiting("auth").AddEndpointFilter<CaptchaFilter>();

        // регистрация с именем пользователя
        app.MapPost("/account/register", async (RegisterRequest req, IAccountService accounts, CancellationToken ct) =>
        {
            var result = await accounts.RegisterAsync(req.Email, req.Password, req.DisplayName, ct);
            return result.Succeeded ? Results.Ok() : Results.ValidationProblem(result.Errors);
        })
        .RequireRateLimiting("auth")
        .AddEndpointFilter<RegistrationGateFilter>()     // выключатель регистрации: ДО captcha
        .AddEndpointFilter<CaptchaFilter>();

        // запрос ссылки для сброса пароля (с проверкой, что адрес зарегистрирован)
        app.MapPost("/account/forgot", async (ForgotRequest req, IAccountService accounts, CancellationToken ct) =>
        {
            var result = await accounts.RequestPasswordResetAsync(req.Email, ct);
            return result switch
            {
                PasswordResetRequestResult.Sent => Results.Ok(),
                PasswordResetRequestResult.NotRegistered => Results.NotFound(new { error = "email_not_registered" }),
                _ => Results.BadRequest(new { error = "email_not_confirmed" })
            };
        })
        .RequireRateLimiting("auth")
        .AddEndpointFilter<CaptchaFilter>();

        // имя и почта текущего пользователя (для верхней панели страницы)
        app.MapGet("/me", async (IAccountService accounts, CancellationToken ct) =>
        {
            var profile = await accounts.GetProfileAsync(ct);
            return profile is null ? Results.Unauthorized() : Results.Ok(profile);
        })
        .RequireAuthorization();

        // выход: отзывает refresh-токены
        app.MapPost("/logout", async (IAccountService accounts, CancellationToken ct) =>
        {
            await accounts.RevokeSessionsAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization();

        return app;
    }
}
