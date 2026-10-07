using System.Text.Json;

namespace MyApp.Web.Services;

public class CaptchaFilter(IHttpClientFactory httpFactory, IConfiguration cfg, ILogger<CaptchaFilter> log) : IEndpointFilter
{
    private static readonly string[] Protected = ["/register", "/login", "/forgotPassword", "/resendConfirmationEmail"];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var path = http.Request.Path.Value ?? "";
        if (!Protected.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase)))
            return await next(ctx);

        var token = http.Request.Headers["X-Captcha-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = "captcha_required" });

        var form = new Dictionary<string, string>
        {
            ["secret"] = cfg["Turnstile:SecretKey"]!,
            ["response"] = token
        };
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (ip is not null) form["remoteip"] = ip;

        try
        {
            var client = httpFactory.CreateClient();
            using var resp = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(form));
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean())
                return await next(ctx);

            log.LogWarning("Captcha не пройдена для {Path} с {Ip}", path, ip);
            return Results.BadRequest(new { error = "captcha_failed" });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Сервис captcha недоступен");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}