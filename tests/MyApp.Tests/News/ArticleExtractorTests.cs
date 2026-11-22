using MyApp.Infrastructure.News;
using Xunit;

namespace MyApp.Tests.News;

public class ArticleExtractorTests
{
    private static readonly Uri Page = new("https://example.com/news/manul-kittens");

    private static string Para(string word) =>
        $"Манулы из зоопарка принесли потомство, и {word} сотрудники рассказали, как проходят первые недели жизни котят.";

    [Fact]
    public void Image_is_taken_from_og_image_and_relative_address_is_resolved()
    {
        var html = "<html><head><meta property=\"og:image\" content=\"/img/manul.jpg\"></head><body></body></html>";
        var page = ArticleExtractor.Parse(html, Page, 5000);
        Assert.Equal("https://example.com/img/manul.jpg", page.ImageUrl);
    }

    [Fact]
    public void Insecure_image_is_ignored_and_twitter_image_is_used_instead()
    {
        var html = "<head><meta property=\"og:image\" content=\"http://example.com/a.jpg\">" +
                   "<meta name=\"twitter:image\" content=\"https://cdn.example.com/b.jpg\"></head>";
        var page = ArticleExtractor.Parse(html, Page, 5000);
        Assert.Equal("https://cdn.example.com/b.jpg", page.ImageUrl);
    }

    [Fact]
    public void Text_comes_from_article_paragraphs_without_menu_and_short_lines()
    {
        var html = "<body><nav><p>Главная страница сайта и длинное описание меню навигации</p></nav>" +
                   $"<article><p>{Para("первый")}</p><p>Читайте также</p><p>{Para("второй")}</p><p>{Para("второй")}</p></article></body>";
        var page = ArticleExtractor.Parse(html, Page, 5000);

        Assert.NotNull(page.Text);
        Assert.DoesNotContain("Главная страница", page.Text);
        Assert.DoesNotContain("Читайте также", page.Text);
        Assert.Equal(2, page.Text!.Split("\n\n").Length);   // одинаковые абзацы не повторяются
    }

    [Fact]
    public void Json_ld_article_body_is_preferred()
    {
        var body = string.Join("\\n", Enumerable.Range(0, 5).Select(i => Para("номер" + i)));
        var html = "<head><script type=\"application/ld+json\">{\"@type\":\"NewsArticle\",\"articleBody\":\"" + body + "\"}</script></head>" +
                   "<body><article><p>Короткий текст, которого не должно быть в результате вообще</p></article></body>";
        var page = ArticleExtractor.Parse(html, Page, 5000);

        Assert.NotNull(page.Text);
        Assert.Contains("номер3", page.Text);
        Assert.DoesNotContain("Короткий текст", page.Text);
    }

    [Fact]
    public void Too_short_page_gives_no_text()
    {
        var html = "<article><p>Эта страница закрыта платной подпиской, доступен только заголовок.</p></article>";
        Assert.Null(ArticleExtractor.Parse(html, Page, 5000).Text);
    }

    [Fact]
    public void Long_text_is_truncated()
    {
        var html = "<article>" + string.Concat(Enumerable.Range(0, 30).Select(i => $"<p>{Para("номер" + i)}</p>")) + "</article>";
        var page = ArticleExtractor.Parse(html, Page, 1000);
        Assert.True(page.Text!.Length <= 1000);
    }

    [Theory]
    [InlineData("93.184.216.34", true)]
    [InlineData("127.0.0.1", false)]
    [InlineData("10.1.2.3", false)]
    [InlineData("192.168.0.5", false)]
    [InlineData("172.20.0.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("::1", false)]
    [InlineData("fd00::1", false)]
    public void Only_public_addresses_are_allowed(string ip, bool expected) =>
        Assert.Equal(expected, PublicNetwork.IsPublic(System.Net.IPAddress.Parse(ip)));

    [Fact]
    public void Bing_redirect_is_unwrapped_to_the_real_article_address()
    {
        var bing = new Uri("http://www.bing.com/news/apiclick.aspx?ref=FexRss&aid=&tid=ABC&url=https%3a%2f%2fexample.com%2fa%3fid%3d5&c=1&mkt=en-us");
        Assert.Equal("https://example.com/a?id=5", NewsPollingService.UnwrapRedirect(bing).AbsoluteUri);

        var plain = new Uri("https://example.com/story");
        Assert.Equal(plain, NewsPollingService.UnwrapRedirect(plain));
    }
}
