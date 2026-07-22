using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoPlatform.Application.Interfaces;

namespace TodoPlatform.Infrastructure.Messaging;

/// <summary>
/// Always writes a structured log; optionally sends SMTP (Mailhog) when <see cref="SmtpOptions.Enabled"/>.
/// </summary>
public sealed class EmailSender(
    IOptions<SmtpOptions> options,
    ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "EmailQueued {To} {Subject}",
            to,
            subject);

        var smtp = options.Value;
        if (!smtp.Enabled)
            return;

        if (!MailAddress.TryCreate(to, out _))
        {
            logger.LogWarning("Skip SMTP — invalid address {To}", to);
            return;
        }

#pragma warning disable SYSLIB0014 // SmtpClient is fine for local Mailhog
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = false
        };
#pragma warning restore SYSLIB0014

        using var message = new MailMessage(smtp.From, to, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
