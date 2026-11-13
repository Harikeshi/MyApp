using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyApp.Application.Abstractions;

namespace MyApp.Infrastructure.Email;

public class ResendEmailGateway(
    IHttpClientFactory httpFactory,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailGateway> log) : IEmailGateway
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var o = options.Value;
        var from = string.IsNullOrWhiteSpace(o.FromName) ? o.FromEmail : $"{o.FromName} <{o.FromEmail}>";

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
        req.Content = JsonContent.Create(new
        {
            from,
            to = new[] { message.To },
            subject = message.Subject,
            html = message.Html,
            text = message.Text
        });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Timeout);

        try
        {
            var http = httpFactory.CreateClient();
            using var resp = await http.SendAsync(req, cts.Token);
            var body = await resp.Content.ReadAsStringAsync(cts.Token);

            if (!resp.IsSuccessStatusCode)
            {
                log.LogError("Resend вернул {Status}: {Body}", (int)resp.StatusCode, body);
                throw new InvalidOperationException($"Письмо не отправлено ({(int)resp.StatusCode})");
            }

            log.LogInformation("Письмо отправлено: {Subject}, ответ {Status}", message.Subject, (int)resp.StatusCode);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            log.LogError("Resend не ответил за {Seconds} секунд", Timeout.TotalSeconds);
            throw;
        }
    }
}
