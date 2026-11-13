using System.Net;
using Microsoft.Extensions.Configuration;

namespace MyApp.Infrastructure.Email;

public static class ResetLink
{
    /// <summary>Параметры стоят после #, поэтому на сервер (и в его журналы) не попадают.</summary>
    public static string Build(IConfiguration cfg, string email, string code)
    {
        var rawCode = WebUtility.HtmlDecode(code);    // Identity присылает код уже закодированным для HTML
        return $"{AppUrls.Base(cfg)}/#reset=1&email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(rawCode)}";
    }
}
