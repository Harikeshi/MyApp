using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using MyApp.Application.Abstractions;
using MyApp.Application.Accounts;
using MyApp.Infrastructure.Email;

namespace MyApp.Infrastructure.Identity;

public class IdentityAccountService(
    UserManager<ApplicationUser> users,
    IEmailSender<ApplicationUser> sender,
    IConfiguration cfg,
    ICurrentUser currentUser) : IAccountService
{
    public async Task<RegisterResult> RegisterAsync(
        string email, string password, string displayName, CancellationToken ct)
    {
        if (!DisplayNameRules.TryNormalize(displayName, out var name))
            return RegisterResult.Failure("DisplayName", DisplayNameRules.ErrorMessage);

        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = name };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            return RegisterResult.Failure(ToErrors(result));

        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = AppUrls.ConfirmEmail(cfg, user.Id, code);

        // Identity ожидает ссылку, уже закодированную для HTML
        await sender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(link));

        return RegisterResult.Success();
    }

    public async Task<PasswordResetRequestResult> RequestPasswordResetAsync(string email, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
            return PasswordResetRequestResult.NotRegistered;
        if (!await users.IsEmailConfirmedAsync(user))
            return PasswordResetRequestResult.NotConfirmed;

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        await sender.SendPasswordResetCodeAsync(user, email, HtmlEncoder.Default.Encode(code));

        return PasswordResetRequestResult.Sent;
    }

    public async Task<ProfileResponse?> GetProfileAsync(CancellationToken ct)
    {
        var user = await users.FindByIdAsync(currentUser.Id);
        if (user is null) return null;

        var email = user.Email ?? "";
        var name = string.IsNullOrWhiteSpace(user.DisplayName) ? email : user.DisplayName;
        return new ProfileResponse(name, email);
    }

    public async Task RevokeSessionsAsync(CancellationToken ct)
    {
        var user = await users.FindByIdAsync(currentUser.Id);
        if (user is not null)
            await users.UpdateSecurityStampAsync(user);
    }

    private static Dictionary<string, string[]> ToErrors(IdentityResult result)
    {
        var errors = new Dictionary<string, string[]>();
        foreach (var e in result.Errors)
        {
            errors[e.Code] = errors.TryGetValue(e.Code, out var old)
                ? old.Append(e.Description).ToArray()
                : new[] { e.Description };
        }
        return errors;
    }
}
