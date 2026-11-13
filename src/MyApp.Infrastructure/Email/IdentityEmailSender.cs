using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyApp.Application.Abstractions;
using MyApp.Application.Accounts;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Email;

/// <summary>Переходник: встроенный Identity просит IEmailSender, а письма строим и отправляем мы.</summary>
public class IdentityEmailSender(IEmailGateway gateway, IConfiguration cfg) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        gateway.SendAsync(AccountEmails.Confirmation(email, confirmationLink));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        gateway.SendAsync(AccountEmails.PasswordReset(email, WebUtility.HtmlDecode(resetLink)));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        gateway.SendAsync(AccountEmails.PasswordReset(email, ResetLink.Build(cfg, email, resetCode)));
}
