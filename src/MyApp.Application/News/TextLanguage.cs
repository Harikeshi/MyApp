namespace MyApp.Application.News;

public static class TextLanguage
{
    /// <summary>
    /// true, если среди букв больше половины кириллических. Нужна, чтобы не платить за «перевод» русских новостей на русский.
    /// </summary>
    public static bool IsMostlyCyrillic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        int letters = 0, cyrillic = 0;
        foreach (var ch in text)
        {
            if (!char.IsLetter(ch)) continue;
            letters++;
            if (ch is >= '\u0400' and <= '\u04FF') cyrillic++;
        }

        return letters > 0 && cyrillic * 2 > letters;
    }
}
