using System.Text.RegularExpressions;

namespace MyApp.Infrastructure.News;

public static class KeywordFilter
{
    /// <summary>
    /// Пустой список: пропускать всё. Иначе нужно целое слово из списка (без учёта регистра),
    /// поэтому "Fed" не сработает на "federal", а "S&amp;P" сработает.
    /// Японский, китайский и корейский пишутся без пробелов, у них «границ слова» нет: такие ключи ищутся как подстрока.
    /// </summary>
    public static Func<string, string?, bool> Create(IReadOnlyCollection<string> keywords)
    {
        if (keywords.Count == 0) return static (_, _) => true;

        var words = keywords.Where(k => !IsCjk(k)).Select(Regex.Escape).ToList();
        var cjk = keywords.Where(IsCjk).Select(Regex.Escape).ToList();

        var parts = new List<string>();
        if (words.Count > 0) parts.Add(@"(?<!\w)(?:" + string.Join("|", words) + @")(?!\w)");
        if (cjk.Count > 0) parts.Add(string.Join("|", cjk));

        var rx = new Regex(string.Join("|", parts), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return (title, summary) => rx.IsMatch(title) || (summary is not null && rx.IsMatch(summary));
    }

    private static bool IsCjk(string s) =>
        s.Any(c => c is >= '\u3040' and <= '\u30FF'     // хирагана и катакана
                 || c is >= '\u3400' and <= '\u9FFF'    // иероглифы
                 || c is >= '\uAC00' and <= '\uD7AF');  // хангыль
}
