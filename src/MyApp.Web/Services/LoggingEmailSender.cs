using Microsoft.AspNetCore.Identity;
using System.Net;

namespace MyApp.Web.Services;

public class LoggingEmailSender(ILogger<LoggingEmailSender> log) : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string link)
    { log.LogInformation("DEV: ссылка подтверждения для {Email}: {Link}", email, WebUtility.HtmlDecode(link)); return Task.CompletedTask; }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string link)
    { log.LogInformation("DEV: ссылка сброса для {Email}: {Link}", email, WebUtility.HtmlDecode(link)); return Task.CompletedTask; }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string code)
    { log.LogInformation("DEV: код сброса для {Email}: {Code}", email, WebUtility.HtmlDecode(code)); return Task.CompletedTask; }
}