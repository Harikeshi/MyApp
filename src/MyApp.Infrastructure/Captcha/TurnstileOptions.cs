namespace MyApp.Infrastructure.Captcha;

public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>Секретный ключ Cloudflare Turnstile. Пусто — captcha отключена (Turnstile__SecretKey).</summary>
    public string SecretKey { get; set; } = "";
}
