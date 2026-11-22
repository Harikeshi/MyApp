using MyApp.Infrastructure.News;
using Xunit;

namespace MyApp.Tests.News;

public class KeywordFilterTests
{
    private static readonly Func<string, string?, bool> Filter = KeywordFilter.Create(["manul", "マヌルネコ", "兔狲"]);

    [Theory]
    [InlineData("神戸どうぶつ王国でマヌルネコの赤ちゃんを公開", true)]   // японский: слово внутри предложения без пробелов
    [InlineData("动物园里的兔狲宝宝首次亮相", true)]
    [InlineData("A manul kitten was born", true)]
    [InlineData("Manuel Neuer saved a penalty", false)]               // латиница: только целое слово
    [InlineData("Bitcoin rises", false)]
    public void Matches_keywords_in_title(string title, bool expected) =>
        Assert.Equal(expected, Filter(title, null));
}
