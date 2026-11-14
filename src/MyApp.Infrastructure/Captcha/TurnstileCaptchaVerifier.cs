using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Application.Abstractions;

namespace MyApp.Infrastructure.Captcha;

public class TurnstileCaptchaVerifier(
    IHttpClientFactory httpFactory,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileCaptchaVerifier> log) : ICaptchaVerifier
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public bool IsEnabled => !string.IsNullOrEmpty(options.Value.SecretKey);

    public async Task<CaptchaVerdict> VerifyAsync(string token, string? remoteIp, CancellationToken ct)
    {
        var form = new Dictionary<string, string>
        {
            ["secret"] = options.Value.SecretKey,
            ["response"] = token
        };
        if (remoteIp is not null) form["remoteip"] = remoteIp;

        try
        {
            var client = httpFactory.CreateClient();
            using var resp = await client.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));

            var root = doc.RootElement;
            if (root.TryGetProperty("success", out var ok) && ok.GetBoolean())
                return CaptchaVerdict.Passed;

            var codes = root.TryGetProperty("error-codes", out var c) ? c.ToString() : "нет";
            log.LogWarning("Turnstile отклонил токен, коды: {Codes}", codes);
            return CaptchaVerdict.Failed;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;                                  // клиент оборвал запрос: решает вызывающий код
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Сервис captcha недоступен");
            return CaptchaVerdict.Unavailable;
        }
    }
}
