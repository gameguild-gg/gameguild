using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using GameGuild.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.API.Email;

public sealed class EmailSender(
    IOptions<EmailDeliveryOptions> options,
    ILogger<EmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Email delivery is disabled. Skipping email to {RecipientEmail}.", message.ToEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(currentOptions.FromEmail))
        {
            throw new InvalidOperationException("EmailDelivery:FromEmail is required to send email.");
        }

        await SendWithSmtpAsync(message, currentOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendWithSmtpAsync(
        EmailMessage message,
        EmailDeliveryOptions currentOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentOptions.SmtpHost))
        {
            throw new InvalidOperationException("EmailDelivery:SmtpHost is required when email delivery uses SMTP.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var smtpClient = new SmtpClient(currentOptions.SmtpHost, currentOptions.SmtpPort)
        {
            EnableSsl = currentOptions.SmtpUseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(currentOptions.SmtpUsername))
        {
            smtpClient.Credentials = new NetworkCredential(
                currentOptions.SmtpUsername,
                currentOptions.SmtpPassword ?? string.Empty);
        }
        else
        {
            smtpClient.UseDefaultCredentials = false;
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(
                currentOptions.FromEmail!,
                string.IsNullOrWhiteSpace(currentOptions.FromName) ? currentOptions.FromEmail : currentOptions.FromName),
            Subject = message.Subject,
            SubjectEncoding = Encoding.UTF8,
            Body = message.PlainTextContent,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false,
        };

        mailMessage.To.Add(new MailAddress(
            message.ToEmail,
            string.IsNullOrWhiteSpace(message.ToName) ? message.ToEmail : message.ToName));

        mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.PlainTextContent,
            Encoding.UTF8,
            MediaTypeNames.Text.Plain));
        mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            message.HtmlContent,
            Encoding.UTF8,
            MediaTypeNames.Text.Html));

        await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Delivered email to {RecipientEmail} through SMTP host {SmtpHost}:{SmtpPort}.",
            message.ToEmail,
            currentOptions.SmtpHost,
            currentOptions.SmtpPort);
    }
}
