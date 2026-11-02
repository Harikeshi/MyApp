using Microsoft.AspNetCore.Identity;
using MyApp.Web.Data;
using System.Net;

namespace MyApp.Web.Services;

public class LoggingEmailSender(ILogger<LoggingEmailSender> log, IConfiguration cfg) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string link)
    {
        log.LogInformation("DEV: ссылка подтверждения для {Email}: {Link}", email, WebUtility.HtmlDecode(link)); 
        return Task.CompletedTask; 
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string link)
    {
        log.LogInformation("DEV: ссылка сброса для {Email}: {Link}", email, WebUtility.HtmlDecode(link)); 
        return Task.CompletedTask; 
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string code)
    {
        log.LogInformation("DEV: ссылка сброса для {Email}: {Link}", email, ResetLink.Build(cfg, email, code));
        return Task.CompletedTask;
    }
}