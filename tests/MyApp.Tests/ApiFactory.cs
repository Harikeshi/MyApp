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
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Persistence; 
using MyApp.Web.Services;

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
            services.AddSingleton<CapturingEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<CapturingEmailSender>());

            // в тестах AiChat не запущен: очистку его данных подменяем заглушкой
            services.AddSingleton<FakeChatDataCleaner>();
            services.AddSingleton<IChatDataCleaner>(sp => sp.GetRequiredService<FakeChatDataCleaner>());
        });
    }

    public CapturingEmailSender Emails => Services.GetRequiredService<CapturingEmailSender>();

    public FakeChatDataCleaner ChatCleaner => Services.GetRequiredService<FakeChatDataCleaner>();

    public const string DefaultPassword = "Test123!xyz";

    public async Task<string> CreateConfirmedUserAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var client = CreateClient();

        (await client.PostAsJsonAsync("/account/register",
             new { email, password = DefaultPassword, displayName = "Test User" })).EnsureSuccessStatusCode();

        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByEmailAsync(email))!;
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        Assert.True((await users.ConfirmEmailAsync(user, token)).Succeeded);
        return email;
    }

    public async Task<LoginTokens> LoginAsync(string email, string password = DefaultPassword)
    {
        var response = await CreateClient().PostAsJsonAsync("/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginTokens>())!;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var tokens = await LoginAsync(await CreateConfirmedUserAsync());
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return client;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    } 

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conn.Dispose();
    }

    public record LoginTokens(string AccessToken, string RefreshToken);

    public class FakeChatDataCleaner : IChatDataCleaner
    {
        public int Calls { get; private set; }
        public bool Result { get; set; } = true;

        public Task<bool> DeleteAllAsync(CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    public class CapturingEmailSender : IEmailSender<ApplicationUser>
    {
        public System.Collections.Concurrent.ConcurrentDictionary<string, string> ResetCodes { get; } = new();

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string link) => Task.CompletedTask;
        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string link) => Task.CompletedTask;

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string code)
        {
            ResetCodes[email] = System.Net.WebUtility.HtmlDecode(code);   // код приходит закодированным для HTML
            return Task.CompletedTask;
        }
    }
}
