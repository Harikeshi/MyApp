using System.Globalization;

namespace MyApp.Application.News;

/// <summary>Keyset-курсор ленты: (PublishedAt, Id). Новые новости не сдвигают страницы, как это бывает с номерами страниц.</summary>
public readonly record struct NewsCursor(DateTimeOffset PublishedAt, long Id)
{
    public string Encode() => $"{PublishedAt.UtcTicks}_{Id}";

    public static bool TryParse(string? s, out NewsCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrEmpty(s)) return false;

        var parts = s.Split('_');
        if (parts.Length != 2) return false;
        if (!long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)) return false;
        if (!long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id)) return false;
        if (ticks > DateTime.MaxValue.Ticks) return false;

        cursor = new NewsCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
