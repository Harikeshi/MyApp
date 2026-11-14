namespace MyApp.Application.Abstractions;

public enum CaptchaVerdict
{
    Passed,
    Failed,
    /// <summary>Сервис проверки не ответил или ответил непонятно.</summary>
    Unavailable
}

/// <summary>Порт проверки captcha. Реализация (Cloudflare Turnstile) лежит в Infrastructure.</summary>
public interface ICaptchaVerifier
{
    /// <summary>false, если captcha не настроена (разработка, тесты): проверку тогда пропускают.</summary>
    bool IsEnabled { get; }

    Task<CaptchaVerdict> VerifyAsync(string token, string? remoteIp, CancellationToken ct);
}
