using Microsoft.AspNetCore.Identity;
using MyApp.Web.Data;
using System.Net;
using System.Net.Http.Headers;

namespace MyApp.Web.Services;

public class ResendEmailSender(IHttpClientFactory httpFactory, IConfiguration cfg, ILogger<ResendEmailSender> log)
    : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string link) =>
    SendAsync(email, "Подтверждение почты на sshch.online",
        html: $"<p>Здравствуйте!</p>" +
              $"<p>Вы зарегистрировались на сайте sshch.online. Чтобы подтвердить адрес, перейдите по ссылке:</p>" +
              $"<p><a href=\"{link}\">Подтвердить почту</a></p>" +
              $"<p>Если ссылка не открывается, скопируйте её в браузер:<br>{link}</p>" +
              $"<p>Если вы не регистрировались, просто проигнорируйте это письмо.</p>",
        text: $"Здравствуйте!\n\nВы зарегистрировались на сайте sshch.online. " +
              $"Чтобы подтвердить адрес, откройте ссылку:\n{WebUtility.HtmlDecode(link)}\n\n" +
              $"Если вы не регистрировались, проигнорируйте это письмо.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string link) =>
        SendAsync(email, "Сброс пароля на sshch.online",
            html: $"<p>Чтобы задать новый пароль, перейдите по <a href=\"{link}\">ссылке</a>.</p>" +
                  $"<p>Если вы не запрашивали сброс, проигнорируйте это письмо.</p>",
            text: $"Чтобы задать новый пароль, откройте ссылку:\n{WebUtility.HtmlDecode(link)}\n\n" +
                  $"Если вы не запрашивали сброс, проигнорируйте это письмо.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string code)
    {
        var link = ResetLink.Build(cfg, email, code);
        var htmlLink = WebUtility.HtmlEncode(link);
        return SendAsync(email, "Сброс пароля на sshch.online",
            html: $"<p>Вы запросили сброс пароля на сайте sshch.online.</p>" +
                  $"<p><a href=\"{htmlLink}\">Задать новый пароль</a></p>" +
                  $"<p>Если ссылка не открывается, скопируйте её в браузер:<br>{htmlLink}</p>" +
                  $"<p>Если вы не запрашивали сброс, проигнорируйте это письмо: пароль не изменится.</p>",
            text: $"Вы запросили сброс пароля на сайте sshch.online.\n\nЗадать новый пароль:\n{link}\n\n" +
                  $"Если вы не запрашивали сброс, проигнорируйте это письмо: пароль не изменится.");
    }
    private async Task SendAsync(string to, string subject, string html, string text)
    {
        var http = httpFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg["Resend:ApiKey"]);
        req.Content = JsonContent.Create(new
        {
            from = $"{cfg["Resend:FromName"]} <{cfg["Resend:FromEmail"]}>",
            to = new[] { to },
            subject,
            html,
            text
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            using var resp = await http.SendAsync(req, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);
            if (!resp.IsSuccessStatusCode)
            {
                log.LogError("Resend вернул {Status}: {Body}", (int)resp.StatusCode, body);
                throw new InvalidOperationException($"Письмо не отправлено ({(int)resp.StatusCode})");
            }
            log.LogInformation("Письмо отправлено: {Subject}, ответ {Status}", subject, (int)resp.StatusCode);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            log.LogError("Resend не ответил за 10 секунд");
            throw;
        }
    }
}