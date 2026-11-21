using System.Net;
using System.Text.RegularExpressions;

namespace MyApp.Infrastructure.News;

/// <summary>Превращает HTML из ленты в чистый текст: клиент выводит его как текст, без HTML.</summary>
public static partial class TextCleaner
{
    [GeneratedRegex(@"<(script|style)\b.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyle();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    public static string? Clean(string? html, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        var text = ScriptStyle().Replace(html, " ");
        text = Tags().Replace(text, " ");
        text = WebUtility.HtmlDecode(text);
        text = Spaces().Replace(text, " ").Trim();

        return text.Length == 0 ? null : Truncate(text, maxLength);
    }

    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)].TrimEnd() + "…";
}
