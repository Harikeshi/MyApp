using System.Text.RegularExpressions;

namespace MyApp.Application.News;

/// <summary>Правила комментариев. Под криптоновостями быстро появляется спам, поэтому ссылки ограничены.</summary>
public static partial class CommentRules
{
    public const int MaxLength = 2000;
    public const int MaxLinks = 1;

    [GeneratedRegex(@"(?:https?://|www\.)\S+", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ManyNewlines();

    public static bool TryNormalize(string? raw, out string text, out string? error)
    {
        var s = (raw ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        s = new string(s.Where(ch => ch == '\n' || !char.IsControl(ch)).ToArray());
        s = ManyNewlines().Replace(s, "\n\n").Trim();
        text = s;

        if (s.Length == 0) { error = "Комментарий пустой."; return false; }
        if (s.Length > MaxLength) { error = $"Максимум {MaxLength} символов."; return false; }
        if (s.Contains("http://", StringComparison.OrdinalIgnoreCase)) { error = "Допускаются только https-ссылки."; return false; }
        if (LinkRegex().Matches(s).Count > MaxLinks) { error = "Не больше одной ссылки в комментарии."; return false; }

        error = null;
        return true;
    }
}
