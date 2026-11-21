using System.Text.RegularExpressions;

namespace MyApp.Infrastructure.News;

public static class KeywordFilter
{
    /// <summary>
    /// Пустой список: пропускать всё. Иначе нужно целое слово из списка (без учёта регистра),
    /// поэтому "Fed" не сработает на "federal", а "S&amp;P" сработает.
    /// </summary>
    public static Func<string, string?, bool> Create(IReadOnlyCollection<string> keywords)
    {
        if (keywords.Count == 0) return static (_, _) => true;

        var pattern = @"(?<!\w)(?:" + string.Join("|", keywords.Select(Regex.Escape)) + @")(?!\w)";
        var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return (title, summary) => rx.IsMatch(title) || (summary is not null && rx.IsMatch(summary));
    }
}
