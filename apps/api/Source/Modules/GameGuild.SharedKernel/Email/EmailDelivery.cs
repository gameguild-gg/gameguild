namespace GameGuild.Email;

public sealed class EmailDeliveryOptions
{
    public bool Enabled { get; set; } = false;

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 1025;

    public string? SmtpUsername { get; set; }

    public string? SmtpPassword { get; set; }

    public bool SmtpUseSsl { get; set; }
}

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string PlainTextContent,
    string HtmlContent,
    string? ToName = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}