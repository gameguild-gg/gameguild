using GameGuild.Commerce.Subscriptions;
using GameGuild.Email;

namespace GameGuild.API.Integration;

public sealed class MonthlyStatementMailSenderAdapter(IEmailSender emailSender) : IMonthlyStatementMailSender
{
    public Task SendAsync(MonthlyStatementEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        return emailSender.SendAsync(
            new EmailMessage(
                message.ToEmail,
                message.Subject,
                message.PlainTextContent,
                message.HtmlContent,
                message.ToName,
                message.Attachments
                    .Select(attachment => new EmailAttachment(
                        attachment.FileName,
                        attachment.ContentType,
                        attachment.Content))
                    .ToList()),
            cancellationToken);
    }
}
