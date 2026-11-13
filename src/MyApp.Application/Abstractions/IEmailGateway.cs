namespace MyApp.Application.Abstractions;

public record EmailMessage(string To, string Subject, string Html, string Text);

/// <summary>Порт отправки писем. Реализации (Resend, журнал для разработки) лежат в Infrastructure.</summary>
public interface IEmailGateway
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
