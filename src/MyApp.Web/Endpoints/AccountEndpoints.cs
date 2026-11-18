using MyApp.Application.Accounts;
using MyApp.Infrastructure.Identity;
using MyApp.Web.Models;
using MyApp.Web.Security;
using MyApp.Web.Services;

namespace MyApp.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccounts(this IEndpointRouteBuilder app)
    {
        // состояние для страницы (открыта ли регистрация)
        app.MapGet("/config", (RegistrationGate gate) => new { registrationEnabled = gate.IsOpen });

        // встроенные эндпоинты Identity: /login, /refresh, /confirmEmail, /resetPassword, ...
        // Captcha проверяет тот же CaptchaFilter (он сам решает по адресу, нужна ли проверка).
        app.MapIdentityApi<ApplicationUser>()
            .RequireRateLimiting("auth")
            .AddEndpointFilter((context, next) =>
                context.HttpContext.RequestServices
                    .GetRequiredService<CaptchaFilter>()
                    .InvokeAsync(context, next));

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

        // смена имени (возвращает обновлённый профиль)
        app.MapPut("/account/name", async (UpdateNameRequest req, IAccountService accounts, CancellationToken ct) =>
        {
            var result = await accounts.UpdateDisplayNameAsync(req.DisplayName, ct);
            return result.Succeeded
                ? Results.Ok(result.Profile)
                : Results.ValidationProblem(new Dictionary<string, string[]> { ["DisplayName"] = [result.Error!] });
        })
        .RequireAuthorization();

        // удаление аккаунта: пароль обязателен; сначала чистим данные чатов в AiChat, потом удаляем пользователя.
        // Неверный пароль отдаём как 400, а не 401: клиент на 401 завершает сессию.
        app.MapPost("/account/delete", async (
            DeleteAccountRequest req, IAccountService accounts, IChatDataCleaner chats, CancellationToken ct) =>
        {
            if (!await accounts.VerifyPasswordAsync(req.Password, ct))
                return Results.BadRequest(new { error = "Неверный пароль." });

            if (!await chats.DeleteAllAsync(ct))
                return Results.Problem(
                    detail: "Не удалось удалить данные чатов. Аккаунт не тронут, попробуйте ещё раз позже.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            return await accounts.DeleteAsync(ct)
                ? Results.NoContent()
                : Results.Problem(detail: "Не удалось удалить аккаунт.", statusCode: StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth");

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
