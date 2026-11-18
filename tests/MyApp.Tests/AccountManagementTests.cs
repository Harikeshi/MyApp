using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MyApp.Tests;

// Смена имени и удаление аккаунта.
public class AccountManagementTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private record Profile(string Name, string Email);

    [Fact]
    public async Task Rename_updates_profile()
    {
        var client = await factory.CreateAuthenticatedClientAsync();
    
        var response = await client.PutAsJsonAsync("/account/name", new { displayName = "  Новое имя  " });
    
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Новое имя", (await response.Content.ReadFromJsonAsync<Profile>())!.Name);
    
        var me = await client.GetFromJsonAsync<Profile>("/me");
        Assert.Equal("Новое имя", me!.Name);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("")]
    [InlineData("имя<script>")]
    public async Task Rename_rejects_invalid_name(string name)
    {
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync("/account/name", new { displayName = name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rename_requires_authentication()
    {
        var response = await factory.CreateClient().PutAsJsonAsync("/account/name", new { displayName = "Иван" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_wrong_password_returns_400_and_keeps_account()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var client = await AuthenticatedAsync(email);
        var callsBefore = factory.ChatCleaner.Calls;

        var response = await client.PostAsJsonAsync("/account/delete", new { password = "wrong-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(callsBefore, factory.ChatCleaner.Calls);   // чаты не трогали
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/me")).StatusCode);
    }

    [Fact]
    public async Task Delete_with_correct_password_removes_account_and_chat_data()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var client = await AuthenticatedAsync(email);
        var callsBefore = factory.ChatCleaner.Calls;

        var response = await client.PostAsJsonAsync("/account/delete", new { password = ApiFactory.DefaultPassword });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(callsBefore + 1, factory.ChatCleaner.Calls);

        var login = await factory.CreateClient().PostAsJsonAsync("/login",
            new { email, password = ApiFactory.DefaultPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Delete_is_aborted_when_chat_data_cannot_be_removed()
    {
        var email = await factory.CreateConfirmedUserAsync();
        var client = await AuthenticatedAsync(email);
        factory.ChatCleaner.Result = false;
        try
        {
            var response = await client.PostAsJsonAsync("/account/delete", new { password = ApiFactory.DefaultPassword });

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/me")).StatusCode);   // аккаунт цел
        }
        finally
        {
            factory.ChatCleaner.Result = true;
        }
    }

    private async Task<HttpClient> AuthenticatedAsync(string email)
    {
        var tokens = await factory.LoginAsync(email);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }
}
