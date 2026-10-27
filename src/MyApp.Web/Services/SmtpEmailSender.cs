using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Http.Headers;
using System.Net;

namespace MyApp.Web.Services;

public class SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> log) : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string link) =>
        SendAsync(email, "Подтвердите почту",
            $"<p>Подтвердите адрес, перейдя по <a href=\"{link}\">ссылке</a>.</p>");

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string link) =>
        SendAsync(email, "Сброс пароля",
            $"<p>Сбросить пароль можно по <a href=\"{link}\">ссылке</a>.</p>");

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string code) =>
        SendAsync(email, "Код сброса пароля", $"<p>Ваш код: {code}</p>");

    private async Task SendAsync(string to, string subject, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(cfg["Smtp:From"]!));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

        var host = cfg["Smtp:Host"]!;
        var port = int.Parse(cfg["Smtp:Port"] ?? "587");

        try
        {
            using var client = new SmtpClient { Timeout = 15_000 };
            await client.ConnectAsync(host, port,
                port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(cfg["Smtp:User"], cfg["Smtp:Password"]);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
            log.LogInformation("Письмо отправлено ({Subject})", subject);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Не удалось отправить письмо ({Subject}) через {Host}", subject, host);
            throw;
        }
    }
}
public class BrevoEmailSender(
    HttpClient httpClient,
    IConfiguration cfg,
    ILogger<BrevoEmailSender> log)
    : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(
        IdentityUser user,
        string email,
        string link)
        => SendAsync(email,
            "Подтвердите почту",
            $"<p>Подтвердите адрес, перейдя по <a href=\"{link}\">ссылке</a>.</p>");

    public Task SendPasswordResetLinkAsync (
            IdentityUser user,
            string email,
            string link)
            => SendAsync(email,
                "Сброс пароля",
                $"<p>Сбросить пароль можно по \"{link}\"ссылке</a>.</p>");

    public Task SendPasswordResetCodeAsync(
        IdentityUser user,
        string email,
        string code)
        => SendAsync(email,
            "Код сброса пароля",
            $"<p>Ваш код: {code}</p>");

    private async Task SendAsync(
        string to,
        string subject,
        string html)
    {
        var apiKey = cfg["Brevo:ApiKey"];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add("api-key", apiKey);

        request.Content = JsonContent.Create(new
        {
            sender = new
            {
                email = cfg["Brevo:From"],
                name = "MyApp"
            },
            to = new[]
            {
                new { email = to }
            },
            subject,
            htmlContent = html
        });

        var response = await httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}
public class SendGridEmailSender(
    IConfiguration cfg,
    ILogger<SendGridEmailSender> log)
    : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(
        IdentityUser user,
        string email,
        string link)
        => SendAsync(
            email,
            "Подтвердите почту",
            $"<p>Подтвердите адрес, перейдя по \"{link}\"ссылке</a>.</p>");

    public Task SendPasswordResetLinkAsync(
        IdentityUser user,
        string email,
        string link)
        => SendAsync(
            email,
            "Сброс пароля",
            $"<p>Сбросить пароль можно по \"{link}\"ссылке</a>.</p>");

    public Task SendPasswordResetCodeAsync(
        IdentityUser user,
        string email,
        string code)
        => SendAsync(
            email,
            "Код сброса пароля",
            $"<p>Ваш код: {code}</p>");

    private async Task SendAsync(
        string to,
        string subject,
        string html)
    {
        try
        {
            var client = new SendGridClient(
                cfg["SendGrid:ApiKey"]);

            log.LogInformation(
    "FromEmail: {FromEmail}",
    cfg["SendGrid:FromEmail"]);

            var message = MailHelper.CreateSingleEmail(
                new EmailAddress(
                    cfg["SendGrid:FromEmail"],
                    cfg["SendGrid:FromName"]),
                new EmailAddress(to),
                subject,
                plainTextContent: null,
                htmlContent: html);

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();

                log.LogError(
                    "SendGrid error. Status={StatusCode}. Body={Body}",
                    response.StatusCode,
                    body);

                throw new Exception(
                    $"SendGrid error {response.StatusCode}: {body}");
            }

            var messageId = response.Headers.TryGetValues("X-Message-Id", out var ids) ? ids.FirstOrDefault() : null;

            log.LogInformation(
                "Письмо отправлено: {Subject}, статус {StatusCode}, id {MessageId}",
                subject, (int)response.StatusCode, messageId); 
        }
        catch (Exception ex)
        {
            log.LogError(
                ex,
                "Ошибка отправки письма");

            throw;
        }
    }
}
public class ResendEmailSender(IHttpClientFactory httpFactory, IConfiguration cfg, ILogger<ResendEmailSender> log)
    : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string link) =>
    SendAsync(email, "Подтверждение почты на sshch.online",
        html: $"<p>Здравствуйте!</p>" +
              $"<p>Вы зарегистрировались на сайте sshch.online. Чтобы подтвердить адрес, перейдите по ссылке:</p>" +
              $"<p><a href=\"{link}\">Подтвердить почту</a></p>" +
              $"<p>Если ссылка не открывается, скопируйте её в браузер:<br>{link}</p>" +
              $"<p>Если вы не регистрировались, просто проигнорируйте это письмо.</p>",
        text: $"Здравствуйте!\n\nВы зарегистрировались на сайте sshch.online. " +
              $"Чтобы подтвердить адрес, откройте ссылку:\n{WebUtility.HtmlDecode(link)}\n\n" +
              $"Если вы не регистрировались, проигнорируйте это письмо.");

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string link) =>
        SendAsync(email, "Сброс пароля на sshch.online",
            html: $"<p>Чтобы задать новый пароль, перейдите по <a href=\"{link}\">ссылке</a>.</p>" +
                  $"<p>Если вы не запрашивали сброс, проигнорируйте это письмо.</p>",
            text: $"Чтобы задать новый пароль, откройте ссылку:\n{WebUtility.HtmlDecode(link)}\n\n" +
                  $"Если вы не запрашивали сброс, проигнорируйте это письмо.");

    //public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string code) =>
    //SendAsync(email, "Код для сброса пароля на sshch.online",
    //    html: $"<p>Ваш код: <b>{code}</b></p>",
    //    text: $"Ваш код: {code}");
    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string code)
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

        using var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            log.LogError("Resend вернул {Status}: {Body}", (int)resp.StatusCode, body);
            throw new InvalidOperationException($"Письмо не отправлено ({(int)resp.StatusCode})");
        }
        log.LogInformation("Письмо отправлено: {Subject}, ответ {Status}", subject, (int)resp.StatusCode);
    }
}