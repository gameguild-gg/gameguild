using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Core.Security;

/// <summary>
/// Queues the "new device signed in" owner notification within the durable consumer's
/// inbox transaction (issue #1085). InApp is always queued; Email only when the user's
/// delivery preferences allow it. The message carries the coarse device summary and
/// coarse GeoIP location of the sign-in, and points the owner at the session
/// management surface where the session can be terminated.
/// </summary>
internal sealed class NewDeviceSignInAlertHandler(
    IUserRepository users,
    INotificationService notifications,
    INotificationPreferenceService preferences,
    IConfiguration configuration) : IIntegrationEventHandler<NewDeviceSignInDetectedV1>
{
    private const string EnabledPath = "Authentication:DeviceNotifications:Enabled";

    /// <summary>The web session-management surface (settings/security, issue #278).</summary>
    private const string SessionsReviewPath = "/workspace/settings/security";

    public async Task HandleAsync(NewDeviceSignInDetectedV1 @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (!configuration.GetValue(EnabledPath, true))
        {
            return;
        }

        var user = await users.GetByIdAsync(@event.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            return;
        }

        var deviceSuffix = string.IsNullOrWhiteSpace(@event.DeviceSummary)
            ? string.Empty
            : $" from {@event.DeviceSummary}";
        var locationSuffix = string.IsNullOrWhiteSpace(@event.LocationSummary)
            ? string.Empty
            : $" near {@event.LocationSummary}";
        var message =
            $"A new device signed in to your account{deviceSuffix}{locationSuffix}. " +
            "If this was not you, terminate the session, change your password and enable multi-factor authentication.";

        await QueueAsync(NotificationChannel.InApp, null).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            // Email follows the user's delivery preferences; a first-seen device is a
            // security-relevant change, so the notification is Urgent and transactional.
            var decision = await preferences.DecideDeliveryAsync(
                user.Id, NotificationType.Security, NotificationChannel.Email, NotificationPriority.Urgent, cancellationToken).ConfigureAwait(false);
            if (decision.Action == NotificationDeliveryAction.Send)
            {
                await QueueAsync(NotificationChannel.Email, user.Email).ConfigureAwait(false);
            }
        }

        async Task QueueAsync(NotificationChannel channel, string? recipientEmail)
        {
            var result = await notifications.SendAsync(user.Id, NotificationType.Security,
                "New device signed in",
                message,
                channel, tenantId: null, actionUrl: SessionsReviewPath,
                priority: NotificationPriority.Urgent,
                referenceEntityId: @event.EventId, referenceEntityType: nameof(NewDeviceSignInDetectedV1),
                recipientEmail: recipientEmail, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result is null || result.IsFailure)
            {
                throw new InvalidOperationException("The new-device sign-in security alert was not durably queued.");
            }
        }
    }
}
