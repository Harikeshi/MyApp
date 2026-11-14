using MyApp.Application.Abstractions;

namespace MyApp.Web.Security;

/// <summary>HTTP-обвязка captcha: достаёт токен из заголовка и спрашивает ICaptchaVerifier (Cloudflare Turnstile).</summary>
public class CaptchaFilter(ICaptchaVerifier captcha, ILogger<CaptchaFilter> log) : IEndpointFilter
{
    // Адреса, где captcha обязательна (встроенные эндпоинты Identity и наши /account/...).
    private static readonly string[] Protected =
    [
        "/login", "/forgotPassword", "/resendConfirmationEmail",
        "/register", "/account/register", "/account/forgot"
    ];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var path = http.Request.Path.Value ?? "";

        // не защищаемый адрес или captcha не настроена (разработка, тесты)
        if (!Protected.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase)) || !captcha.IsEnabled)
            return await next(ctx);

        var token = http.Request.Headers["X-Captcha-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = "captcha_required" });

        var ip = http.Connection.RemoteIpAddress?.ToString();

        // Под try только проверка captcha: next() вызывается ПОСЛЕ, иначе ошибки самого эндпоинта
        // считались бы сбоем captcha.
        CaptchaVerdict verdict;
        try
        {
            verdict = await captcha.VerifyAsync(token, ip, http.RequestAborted);
        }
        catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }

        switch (verdict)
        {
            case CaptchaVerdict.Unavailable:
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            case CaptchaVerdict.Failed:
                log.LogWarning("Captcha не пройдена для {Path} с {Ip}", path, ip);
                return Results.BadRequest(new { error = "captcha_failed" });

            default:
                return await next(ctx);
        }
    }
}
