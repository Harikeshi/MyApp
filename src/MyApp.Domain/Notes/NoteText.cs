using System.Text.RegularExpressions;

namespace MyApp.Domain.Notes;

/// <summary>Текст записи без разметки и картинок: по нему идёт поиск и строится превью списка.</summary>
public static class NoteText
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
    private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex Scripts = new(@"<(script|style)\b[^>]*>.*?</\1\s*>", Opt | RegexOptions.Singleline, Timeout);
    private static readonly Regex Images = new(@"<img\b[^>]*>", Opt, Timeout);
    private static readonly Regex Breaks = new(@"<(?:br\s*/?|/p|/div|/li|/h[1-6]|/blockquote)\s*>", Opt, Timeout);
    private static readonly Regex Tags = new(@"<[^>]*>", Opt, Timeout);
    private static readonly Regex Spaces = new(@"\s+", Opt, Timeout);

    public static string ToPlain(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        var text = Scripts.Replace(html, " ");
        text = Images.Replace(text, " ");          // base64 картинок не должен попадать в текст
        text = Breaks.Replace(text, " ");
        text = Tags.Replace(text, "");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Spaces.Replace(text, " ").Trim();
    }
}
