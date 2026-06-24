using GameGuild.Commerce.Subscriptions;
using GameGuild.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.Integration;

public sealed class MonthlyStatementMailSenderAdapter(
    IServiceProvider serviceProvider,
    ILogger<MonthlyStatementMailSenderAdapter> logger) : IMonthlyStatementMailSender
{
    public async Task SendAsync(MonthlyStatementEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var emailSender = serviceProvider.GetService<IEmailSender>();
        if (emailSender is null)
        {
            logger.LogInformation(
                "Monthly statement email requested for {RecipientEmail}, but no email sender is configured.",
                message.ToEmail);
            return;
        }

        await emailSender.SendAsync(
            new EmailMessage(
                message.ToEmail,
                message.Subject,
                message.PlainTextContent,
                message.HtmlContent,
                message.ToName),
            cancellationToken).ConfigureAwait(false);
    }
}
