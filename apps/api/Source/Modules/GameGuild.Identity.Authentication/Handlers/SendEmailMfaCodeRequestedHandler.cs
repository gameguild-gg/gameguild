using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handles one-time email-MFA code delivery by recording a durable email notification row
///     (issue #1152). The notification sweep delivers it through the configured email provider.
/// </summary>
public sealed class SendEmailMfaCodeRequestedHandler(
    ILogger<SendEmailMfaCodeRequestedHandler> logger,
    INotificationService notificationService) : INotificationHandler<EmailMfaCodeRequestedNotification>
{
    public async Task Handle(EmailMfaCodeRequestedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                code = notification.Code,
                email = notification.Email,
                isEnrollment = notification.IsEnrollment
            });

            var result = await notificationService.SendAsync(
                notification.UserId,
                NotificationType.EmailCode,
                "Your GameGuild verification code",
                $"Your GameGuild verification code is {notification.Code}. It expires in 10 minutes. Never share this code.",
                NotificationChannel.Email,
                tenantId: null,
                priority: NotificationPriority.High,
                metadata: metadata,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result is null || result.IsFailure)
            {
                throw new InvalidOperationException("Authentication notification was not durably queued.");
            }

            logger.LogInformation("Email MFA code queued for user {UserId}",
                LogRedaction.RedactId(notification.UserId, "uid"));
        }
        catch (Exception ex)
        {
            logger.LogError("Error queueing email MFA code for user {UserId}: {ErrorType}",
                LogRedaction.RedactId(notification.UserId, "uid"), ex.GetType().FullName);
            throw; // Queue persistence failed; do not acknowledge a lost notification.
        }
    }
}
