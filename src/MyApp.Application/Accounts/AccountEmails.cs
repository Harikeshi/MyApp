using System.Net;
using MyApp.Application.Abstractions;

namespace MyApp.Application.Accounts;

/// <summary>Тексты писем. Сама отправка (Resend и т. д.) находится в Infrastructure.</summary>
public static class AccountEmails
{
    public const string SiteName = "sshch.online";

    /// <param name="linkHtmlEncoded">Ссылка в том виде, в каком её передаёт Identity: уже закодирована для HTML.</param>
    public static EmailMessage Confirmation(string to, string linkHtmlEncoded)
    {
        var html =
            "<p>Здравствуйте!</p>" +
            $"<p>Вы зарегистрировались на сайте {SiteName}. Чтобы подтвердить адрес, перейдите по ссылке:</p>" +
            $"<p><a href=\"{linkHtmlEncoded}\">Подтвердить почту</a></p>" +
            $"<p>Если ссылка не открывается, скопируйте её в браузер:<br>{linkHtmlEncoded}</p>" +
            "<p>Если вы не регистрировались, просто проигнорируйте это письмо.</p>";

        var text =
            "Здравствуйте!\n\n" +
            $"Вы зарегистрировались на сайте {SiteName}. Чтобы подтвердить адрес, откройте ссылку:\n" +
            $"{WebUtility.HtmlDecode(linkHtmlEncoded)}\n\n" +
            "Если вы не регистрировались, проигнорируйте это письмо.";

        return new EmailMessage(to, $"Подтверждение почты на {SiteName}", html, text);
    }

    /// <param name="link">Обычная (не закодированная для HTML) ссылка.</param>
    public static EmailMessage PasswordReset(string to, string link)
    {
        var htmlLink = WebUtility.HtmlEncode(link);

        var html =
            $"<p>Вы запросили сброс пароля на сайте {SiteName}.</p>" +
            $"<p><a href=\"{htmlLink}\">Задать новый пароль</a></p>" +
            $"<p>Если ссылка не открывается, скопируйте её в браузер:<br>{htmlLink}</p>" +
            "<p>Если вы не запрашивали сброс, проигнорируйте это письмо: пароль не изменится.</p>";

        var text =
            $"Вы запросили сброс пароля на сайте {SiteName}.\n\n" +
            $"Задать новый пароль:\n{link}\n\n" +
            "Если вы не запрашивали сброс, проигнорируйте это письмо: пароль не изменится.";

        return new EmailMessage(to, $"Сброс пароля на {SiteName}", html, text);
    }
}
