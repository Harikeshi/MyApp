using MyApp.Application.News;
using MyApp.Infrastructure.News;
using Xunit;

namespace MyApp.Tests.News;

public class NewsRulesTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("   \n  ", false)]
    [InlineData("Нормальный комментарий", true)]
    [InlineData("Смотри https://example.com/a", true)]
    [InlineData("Две ссылки https://a.com и https://b.com", false)]
    [InlineData("Небезопасная http://a.com", false)]
    [InlineData("Ссылка без схемы www.a.com и ещё www.b.com", false)]
    public void Comment_rules(string input, bool valid) =>
        Assert.Equal(valid, CommentRules.TryNormalize(input, out _, out _));

    [Fact]
    public void Comment_too_long_is_rejected() =>
        Assert.False(CommentRules.TryNormalize(new string('a', CommentRules.MaxLength + 1), out _, out _));

    [Fact]
    public void Comment_is_trimmed_and_newlines_collapsed()
    {
        Assert.True(CommentRules.TryNormalize("  a\r\n\r\n\r\n\r\nb  ", out var text, out _));
        Assert.Equal("a\n\nb", text);
    }

    [Fact]
    public void Cursor_roundtrip()
    {
        var c = new NewsCursor(new DateTimeOffset(2026, 11, 20, 10, 30, 0, TimeSpan.Zero), 42);
        Assert.True(NewsCursor.TryParse(c.Encode(), out var parsed));
        Assert.Equal(c, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1_2_3")]
    [InlineData("-1_5")]
    public void Cursor_garbage_is_rejected(string? s) =>
        Assert.False(NewsCursor.TryParse(s, out _));

    [Fact]
    public void Keyword_filter_matches_whole_words_only()
    {
        var pass = KeywordFilter.Create(["Fed", "S&P"]);
        Assert.True(pass("Fed holds rates", null));
        Assert.True(pass("Index", "S&P 500 rises"));
        Assert.False(pass("Federal grant announced", "Nothing relevant"));
    }

    [Fact]
    public void Empty_keyword_list_passes_everything() =>
        Assert.True(KeywordFilter.Create([])("anything", null));

    [Fact]
    public void Text_cleaner_strips_html_and_truncates()
    {
        Assert.Equal("Bitcoin & ETF news", TextCleaner.Clean("<p>Bitcoin &amp; ETF <script>alert(1)</script>news</p>", 100));
        Assert.Equal("abcd…", TextCleaner.Truncate("abcdefgh", 5));
    }

    [Fact]
    public void Keyword_filter_handles_russian_word_forms()
    {
        var pass = KeywordFilter.Create(["манул", "манулы", "Pallas's cat"]);
        Assert.True(pass("В зоопарке родились манулы", null));
        Assert.True(pass("Pallas's cat spotted in Mongolia", null));
        Assert.False(pass("Манулярный эффект", null));   // целое слово, не подстрока
    }

    [Theory]
    [InlineData("В зоопарке родились манулы", true)]
    [InlineData("Pallas's cat kittens born", false)]
    [InlineData("Bitcoin и ETF", false)]
    [InlineData("", false)]
    public void Cyrillic_detection(string text, bool expected) =>
        Assert.Equal(expected, TextLanguage.IsMostlyCyrillic(text));
}
