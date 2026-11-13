using Microsoft.Extensions.Logging;
using MyApp.Application.Abstractions;

namespace MyApp.Infrastructure.Email;

/// <summary>Для разработки и временного режима: письмо не уходит, а пишется в журнал (ищите «DEV:»).</summary>
public class LoggingEmailGateway(ILogger<LoggingEmailGateway> log) : IEmailGateway
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        log.LogInformation("DEV: письмо для {To}: {Subject}\n{Text}", message.To, message.Subject, message.Text);
        return Task.CompletedTask;
    }
}
