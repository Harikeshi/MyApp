using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MyApp.Infrastructure.News;

/// <param name="ImageUrl">Только https, абсолютный адрес; null: фото не нашли.</param>
/// <param name="Text">Абзацы через пустую строку; null: текста нет или он слишком короткий (платный доступ, страница на JS).</param>
public sealed record ArticlePage(string? ImageUrl, string? Text);

/// <summary>Достаёт со страницы статьи главное фото (og:image) и текст. Без внешних сервисов, только разбор HTML.</summary>
public static partial class ArticleExtractor
{
    /// <summary>Меньше этого текст считаем неудачей: скорее всего вместо статьи пришла заглушка.</summary>
    private const int MinTextLength = 200;

    /// <summary>Абзацы короче этого — меню, подписи, кнопки.</summary>
    private const int MinParagraphLength = 40;

    private static readonly string[] ImageMetaKeys = ["og:image:secure_url", "og:image", "twitter:image", "twitter:image:src"];

    [GeneratedRegex(@"(^|[\s_-])(comments?|related|promo|newsletter|subscribe|share|social|sidebar|widget|advert|recommended|cookie|banner)([\s_-]|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex JunkClass();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    public static ArticlePage Parse(string html, Uri pageUrl, int maxTextLength)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var image = FindImage(doc, pageUrl);
        var text = FindJsonLdText(doc) ?? FindHtmlText(doc);

        if (text is not null && text.Length < MinTextLength) text = null;
        if (text is not null) text = TextCleaner.Truncate(text, maxTextLength);
        return new ArticlePage(image, text);
    }

    private static string? FindImage(HtmlDocument doc, Uri pageUrl)
    {
        foreach (var key in ImageMetaKeys)
        {
            var meta = doc.DocumentNode.SelectSingleNode($"//meta[@property='{key}' or @name='{key}']");
            var raw = meta?.Attributes["content"]?.Value;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            // Адрес бывает относительным («/img/a.jpg») или с «//host/...»: разбираем относительно страницы.
            if (Uri.TryCreate(pageUrl, WebUtility.HtmlDecode(raw.Trim()), out var abs)
                && abs.Scheme == Uri.UriSchemeHttps
                && abs.AbsoluteUri.Length <= 2000)
                return abs.AbsoluteUri;
        }
        return null;
    }

    /// <summary>Многие издания кладут полный текст в JSON-LD (articleBody): это самый чистый источник.</summary>
    private static string? FindJsonLdText(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scripts is null) return null;

        foreach (var script in scripts)
        {
            try
            {
                using var json = JsonDocument.Parse(script.InnerText);
                var body = FindArticleBody(json.RootElement);
                if (string.IsNullOrWhiteSpace(body)) continue;

                // В articleBody иногда попадает разметка.
                var frag = new HtmlDocument();
                frag.LoadHtml(body);
                var plain = WebUtility.HtmlDecode(frag.DocumentNode.InnerText);

                var paragraphs = plain.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => Spaces().Replace(p, " ").Trim())
                    .Where(p => p.Length > 0)
                    .ToList();
                var joined = string.Join("\n\n", paragraphs);
                if (joined.Length >= MinTextLength) return joined;
            }
            catch (JsonException) { /* битый JSON-LD: пробуем следующий или разбираем HTML */ }
        }
        return null;
    }

    private static string? FindArticleBody(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject())
                {
                    if (p.NameEquals("articleBody") && p.Value.ValueKind == JsonValueKind.String) return p.Value.GetString();
                    if (FindArticleBody(p.Value) is { } found) return found;
                }
                return null;
            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    if (FindArticleBody(item) is { } found) return found;
                return null;
            default:
                return null;
        }
    }

    private static string? FindHtmlText(HtmlDocument doc)
    {
        var root = doc.DocumentNode;

        // Убираем всё, что точно не текст статьи.
        var trash = root.SelectNodes("//script|//style|//noscript|//nav|//aside|//footer|//form|//figure|//iframe|//svg|//button")?.ToList();
        if (trash is not null) foreach (var n in trash) n.Remove();

        // Контейнер: самый «текстовый» <article>, иначе <main>, иначе вся страница.
        var container = root.SelectNodes("//article")?.OrderByDescending(a => a.InnerText.Length).FirstOrDefault()
                        ?? root.SelectSingleNode("//main")
                        ?? root.SelectSingleNode("//body")
                        ?? root;
        var checkJunk = container != root.SelectSingleNode("//body");

        var seen = new HashSet<string>();
        var paragraphs = new List<string>();
        var nodes = container.SelectNodes(".//p");
        if (nodes is null) return null;

        foreach (var p in nodes)
        {
            var text = Spaces().Replace(WebUtility.HtmlDecode(p.InnerText), " ").Trim();
            if (text.Length < MinParagraphLength) continue;
            if (checkJunk && InJunk(p, container)) continue;
            if (seen.Add(text)) paragraphs.Add(text);
        }

        return paragraphs.Count == 0 ? null : string.Join("\n\n", paragraphs);
    }

    private static bool InJunk(HtmlNode node, HtmlNode root)
    {
        for (var a = node.ParentNode; a is not null && a != root; a = a.ParentNode)
        {
            var marks = a.GetAttributeValue("class", "") + " " + a.GetAttributeValue("id", "");
            if (JunkClass().IsMatch(marks)) return true;
        }
        return false;
    }
}
