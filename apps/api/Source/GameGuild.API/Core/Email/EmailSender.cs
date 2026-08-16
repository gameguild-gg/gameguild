using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using GameGuild.Email;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGridEmailAddress = SendGrid.Helpers.Mail.EmailAddress;

namespace GameGuild.API.Email;

public sealed class EmailSender : IEmailSender
{
    private readonly Func<string, ISendGridClient> sendGridClientFactory;
    private readonly IOptions<EmailDeliveryOptions> options;
    private readonly ILogger<EmailSender> logger;

    public EmailSender(
        IOptions<EmailDeliveryOptions> options,
        ILogger<EmailSender> logger)
        : this(options, logger, apiKey => new SendGridClient(apiKey))
    {
    }

    internal EmailSender(
        IOptions<EmailDeliveryOptions> options,
        ILogger<EmailSender> logger,
        Func<string, ISendGridClient> sendGridClientFactory)
    {
        this.options = options;
        this.logger = logger;
        this.sendGridClientFactory = sendGridClientFactory;
    }

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

        var provider = ResolveProvider(currentOptions);
        if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            await SendWithSmtpAsync(message, currentOptions, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            await SendWithSendGridAsync(message, currentOptions, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new InvalidOperationException(
            $"EmailDelivery:Provider '{currentOptions.Provider}' is not supported. Use 'SendGrid' or 'Smtp'.");
    }

    private static string ResolveProvider(EmailDeliveryOptions currentOptions)
    {
        if (!string.IsNullOrWhiteSpace(currentOptions.Provider))
        {
            return currentOptions.Provider.Trim();
        }

        return string.IsNullOrWhiteSpace(currentOptions.SendGridApiKey) ? "Smtp" : "SendGrid";
    }

    private async Task SendWithSendGridAsync(
        EmailMessage message,
        EmailDeliveryOptions currentOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(currentOptions.SendGridApiKey))
        {
            throw new InvalidOperationException("EmailDelivery:SendGridApiKey is required when email delivery uses SendGrid.");
        }

        var client = sendGridClientFactory(currentOptions.SendGridApiKey);
        var from = new SendGridEmailAddress(
            currentOptions.FromEmail,
            string.IsNullOrWhiteSpace(currentOptions.FromName) ? currentOptions.FromEmail : currentOptions.FromName);
        var to = new SendGridEmailAddress(
            message.ToEmail,
            string.IsNullOrWhiteSpace(message.ToName) ? message.ToEmail : message.ToName);
        var providerMessage = MailHelper.CreateSingleEmail(
            from,
            to,
            message.Subject,
            message.PlainTextContent,
            message.HtmlContent);

        foreach (var attachment in message.Attachments ?? [])
        {
            providerMessage.AddAttachment(
                attachment.FileName,
                Convert.ToBase64String(attachment.Content),
                attachment.ContentType);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var response = await client.SendEmailAsync(providerMessage, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"SendGrid rejected email delivery with status code {(int)response.StatusCode}.");
        }

        logger.LogInformation(
            "Delivered email to {RecipientEmail} with {AttachmentCount} attachments.",
            message.ToEmail,
            message.Attachments?.Count ?? 0);
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
            DeliveryMethod = SmtpDeliveryMethod.Network
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
            IsBodyHtml = false
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

        foreach (var attachment in message.Attachments ?? [])
        {
            var stream = new MemoryStream(attachment.Content, writable: false);
            mailMessage.Attachments.Add(
                new System.Net.Mail.Attachment(stream, attachment.FileName, attachment.ContentType));
        }

        await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Delivered email to {RecipientEmail} through SMTP host {SmtpHost}:{SmtpPort} with {AttachmentCount} attachments.",
            message.ToEmail,
            currentOptions.SmtpHost,
            currentOptions.SmtpPort,
            message.Attachments?.Count ?? 0);
    }
}
