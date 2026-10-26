using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Web.Data;

namespace MyApp.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("RateLimit:AuthPerMinute", "100000");

        builder.ConfigureTestServices(services =>
        {
            _conn.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
            services.AddSingleton<IEmailSender<IdentityUser>, NoOpEmailSender>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    // Регистрирует пользователя, подтверждает почту, входит и возвращает клиента с токеном
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Test123!xyz";
        var client = CreateClient();

        (await client.PostAsJsonAsync("/register", new { email, password })).EnsureSuccessStatusCode();

        using (var scope = Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = (await users.FindByEmailAsync(email))!;
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            (await users.ConfirmEmailAsync(user, token)).Succeeded.ShouldBeTrue();
        }

        var login = await client.PostAsJsonAsync("/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conn.Dispose();
    }

    private record LoginResponse(string AccessToken);

    private class NoOpEmailSender : IEmailSender<IdentityUser>
    {
        public Task SendConfirmationLinkAsync(IdentityUser user, string email, string link) => Task.CompletedTask;
        public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string link) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string code) => Task.CompletedTask;
    }
}

file static class Extensions
{
    public static void ShouldBeTrue(this bool value) => Assert.True(value);
}