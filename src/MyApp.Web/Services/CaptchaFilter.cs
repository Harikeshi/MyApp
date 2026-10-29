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

        var secret = cfg["Turnstile:SecretKey"];
        if (string.IsNullOrEmpty(secret))
            return await next(ctx);                       // captcha не настроена

        var token = http.Request.Headers["X-Captcha-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
            return Results.BadRequest(new { error = "captcha_required" });

        var form = new Dictionary<string, string> { ["secret"] = secret, ["response"] = token };
        var ip = http.Connection.RemoteIpAddress?.ToString();
        if (ip is not null) form["remoteip"] = ip;

        bool success;
        var codes = "нет";
        var ct = http.RequestAborted;
        try
        {
            var client = httpFactory.CreateClient();            
            using var resp = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(form), ct);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            success = doc.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean();
            if (!success && doc.RootElement.TryGetProperty("error-codes", out var c))
                codes = c.ToString();
        }
        // todo: catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Сервис captcha недоступен");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (!success)
        {
            log.LogWarning("Captcha не пройдена для {Path} с {Ip}, коды: {Codes}", path, ip, codes);
            return Results.BadRequest(new { error = "captcha_failed" });
        }

        return await next(ctx);
    }
}