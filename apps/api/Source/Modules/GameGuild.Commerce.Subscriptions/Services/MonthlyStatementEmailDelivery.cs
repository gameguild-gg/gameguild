using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

public sealed record MonthlyStatementEmailAttachment(string FileName, string ContentType, byte[] Content);

public sealed record MonthlyStatementEmailMessage(
    string ToEmail,
    string Subject,
    string PlainTextContent,
    string HtmlContent,
    IReadOnlyList<MonthlyStatementEmailAttachment> Attachments,
    string? ToName = null);

public interface IMonthlyStatementMailSender
{
    Task SendAsync(MonthlyStatementEmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class LoggingMonthlyStatementMailSender(
    ILogger<LoggingMonthlyStatementMailSender> logger) : IMonthlyStatementMailSender
{
    public Task SendAsync(MonthlyStatementEmailMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation(
            "Monthly statement email prepared for {RecipientEmail} with {AttachmentCount} attachments",
            message.ToEmail,
            message.Attachments.Count);

        return Task.CompletedTask;
    }
}
