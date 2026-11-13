using Microsoft.Extensions.Configuration;

namespace MyApp.Infrastructure.Email;

public static class AppUrls
{
    public static string Base(IConfiguration cfg) =>
        (cfg["App:BaseUrl"] ?? "http://localhost:5247").TrimEnd('/');

    public static string ConfirmEmail(IConfiguration cfg, string userId, string code) =>
        $"{Base(cfg)}/confirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";
}
