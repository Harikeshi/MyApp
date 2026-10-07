using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using MimeKit;

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

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(cfg["Smtp:Host"], int.Parse(cfg["Smtp:Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(cfg["Smtp:User"], cfg["Smtp:Password"]);
            await client.SendAsync(msg);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Не удалось отправить письмо ({Subject})", subject);
            throw;
        }
    }
}