namespace GameGuild.Email;

public sealed class EmailDeliveryOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string? SendGridApiKey { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 1025;

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public bool SmtpUseSsl { get; set; }
}

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string PlainTextContent,
    string HtmlContent,
    string? ToName = null,
    IReadOnlyList<EmailAttachment>? Attachments = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
