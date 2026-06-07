using GameGuild.CQRS;
using GameGuild.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for user signed up notifications - sends welcome email.
/// </summary>
public sealed class SendWelcomeEmailHandler(
    ILogger<SendWelcomeEmailHandler> logger,
    IEmailSender? emailSender = null,
    IConfiguration? configuration = null) : INotificationHandler<UserSignedUpNotification>
{
    public async Task Handle(UserSignedUpNotification notification, CancellationToken cancellationToken)
    {
        if (emailSender == null)
        {
            logger.LogInformation("Welcome email requested for user {Email} (ID: {UserId}) — no email service configured",
                notification.Email, notification.UserId);
            return;
        }

        try
        {
            var appBaseUrl = configuration?["App:BaseUrl"] ?? "http://localhost:3000";
            var displayName = string.IsNullOrWhiteSpace(notification.Username) ? notification.Email : notification.Username;
            var plainTextContent =
                $"Hi {displayName},\n\nWelcome to GameGuild. Your account is ready to use.\n\nSign in: {appBaseUrl.TrimEnd('/')}\n\nSee you there.";
            var htmlContent =
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(displayName)},</p><p>Welcome to GameGuild. Your account is ready to use.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(appBaseUrl.TrimEnd('/'))}\">Open GameGuild</a></p><p>See you there.</p>";

            await emailSender.SendAsync(
                new EmailMessage(
                    notification.Email,
                    "Welcome to GameGuild",
                    plainTextContent,
                    htmlContent,
                    displayName),
                cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Welcome email delivered for user {Email} (ID: {UserId})", notification.Email, notification.UserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Welcome email delivery failed for user {Email} (ID: {UserId})", notification.Email, notification.UserId);
        }
    }
}
