using System.Net;

namespace MyApp.Web.Services;

public static class ResetLink
{
    public static string Build(IConfiguration cfg, string email, string code)
    {
        var baseUrl = (cfg["App:BaseUrl"] ?? "http://localhost:5247").TrimEnd('/');
        var rawCode = WebUtility.HtmlDecode(code);        // Identity присылает код уже закодированным для HTML
        return $"{baseUrl}/#reset=1&email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(rawCode)}";
    }
}