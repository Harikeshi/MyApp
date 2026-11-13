using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using MyApp.Infrastructure.Email;

namespace MyApp.Tests;


// *Нет утечки адресов: сброс для несуществующей почты отвечает так же, как для настоящей, и письма не создаёт.
// *Полный путь сброса: новый пароль работает, старый перестаёт, а повторное использование кода даёт 400.
// *Выход: после /logout refresh-токен перестаёт обновлять сессию.
// *Ссылка: код и адрес находятся после (то есть не попадают в журналы сервера) и экранируются. Заметьте, что в тесте ссылки код передаётся уже закодированным (&amp;), как его присылает Identity.
public class AuthFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task ForgotPassword_for_unknown_email_returns_200_without_revealing_anything()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/forgotPassword", new { email = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(factory.Emails.ResetCodes.ContainsKey("nobody@example.com"));
    }

    [Fact]
    public async Task Password_reset_changes_password_and_code_is_single_use()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/forgotPassword", new { email })).EnsureSuccessStatusCode();
        var code = factory.Emails.ResetCodes[email];

        const string newPassword = "Brand-new1!pass";
        var reset = await client.PostAsJsonAsync("/resetPassword",
            new { email, resetCode = code, newPassword });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var oldLogin = await client.PostAsJsonAsync("/login", new { email, password = ApiFactory.DefaultPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/login", new { email, password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);

        var reuse = await client.PostAsJsonAsync("/resetPassword",
            new { email, resetCode = code, newPassword = "Another-one2!pass" });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
    }

    [Fact]
    public async Task Reset_with_wrong_code_returns_400()
    {
        var email = await factory.CreateConfirmedUserAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/resetPassword",
            new { email, resetCode = "wrong-code", newPassword = "Brand-new1!pass" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_invalidates_the_refresh_token()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var tokens = await factory.LoginAsync(email);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var logout = await client.PostAsync("/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await factory.CreateClient()
            .PostAsJsonAsync("/refresh", new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Logout_without_token_returns_401()
    {
        var response = await factory.CreateClient().PostAsync("/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ResetLink_puts_parameters_into_the_fragment_and_escapes_them()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = "https://example.test/" })
            .Build();

        var link = ResetLink.Build(cfg, "a+b@example.com", "abc&amp;def");

        Assert.Equal("https://example.test/#reset=1&email=a%2Bb%40example.com&code=abc%26def", link);
    }

    /*  /ME */
    [Fact]
    public async Task Register_without_display_name_returns_400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/account/register",
            new { email = $"{Guid.NewGuid():N}@example.com", password = ApiFactory.DefaultPassword });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Old_register_endpoint_is_closed()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/register",
            new { email = "a@example.com", password = ApiFactory.DefaultPassword });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_display_name_and_email()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var tokens = await factory.LoginAsync(email);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        var me = await client.GetFromJsonAsync<MeResponse>("/me");

        Assert.Equal("Test User", me!.Name);
        Assert.Equal(email, me.Email);
    }

    private record MeResponse(string Name, string Email);
}