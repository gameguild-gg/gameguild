using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;

namespace GameGuild.Identity.Authentication;

/// <summary>Queues the code only inside protected identity-email metadata.</summary>
public sealed class SendOrchestratedRiskCodeRequestedHandler(
    ILogger<SendOrchestratedRiskCodeRequestedHandler> logger,
    INotificationService notifications) : INotificationHandler<OrchestratedRiskCodeRequestedNotification>
{
    public async Task Handle(OrchestratedRiskCodeRequestedNotification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var metadata = JsonSerializer.Serialize(new
            {
                code = notification.Code, email = notification.Email,
                purpose = "orchestrated-risk", expiresAt = notification.ExpiresAt
            });
            var result = await notifications.SendAsync(notification.UserId, NotificationType.EmailCode,
                "Verify your sign-in", "A sign-in verification code was requested.", NotificationChannel.Email,
                priority: NotificationPriority.High, metadata: metadata, recipientEmail: notification.Email,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result is null || result.IsFailure)
            {
                throw new InvalidOperationException("Authentication notification was not durably queued.");
            }
            logger.LogInformation("Sign-in risk code queued for user {UserId}", LogRedaction.RedactId(notification.UserId, "uid"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError("Could not queue sign-in risk code for user {UserId}: {ErrorType}",
                LogRedaction.RedactId(notification.UserId, "uid"), exception.GetType().FullName);
            throw;
        }
    }
}
