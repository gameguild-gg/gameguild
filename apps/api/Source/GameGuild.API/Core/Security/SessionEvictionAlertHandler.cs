using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Core.Security;

/// <summary>
/// Queues the "a session was signed out because you reached the concurrent session
/// limit" owner notification within the durable consumer's inbox transaction
/// (issue #1086). InApp is always queued; Email only when the user's delivery
/// preferences allow it. Points the owner at the session management surface where the
/// remaining sessions can be reviewed and terminated.
/// </summary>
internal sealed class SessionEvictionAlertHandler(
    IUserRepository users,
    INotificationService notifications,
    INotificationPreferenceService preferences,
    IConfiguration configuration) : IIntegrationEventHandler<SessionEvictionDetectedV1>
{
    private const string EnabledPath = "Authentication:SessionNotifications:Enabled";

    /// <summary>The web session-management surface (settings/security, issue #278).</summary>
    private const string SessionsReviewPath = "/workspace/settings/security";

    public async Task HandleAsync(SessionEvictionDetectedV1 @event, CancellationToken cancellationToken = default)
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
            : $" ({@event.DeviceSummary})";
        var message =
            $"You are signed in on more than {@event.MaxConcurrentSessions} devices, so your oldest session{deviceSuffix} was signed out. " +
            "If you did not expect this, review your active sessions and sign out anything you do not recognize.";

        await QueueAsync(NotificationChannel.InApp, null).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            // Informational notice: the user's channel toggles, mutes and quiet hours apply.
            var decision = await preferences.DecideDeliveryAsync(
                user.Id, NotificationType.Security, NotificationChannel.Email, NotificationPriority.Normal, cancellationToken).ConfigureAwait(false);
            if (decision.Action == NotificationDeliveryAction.Send)
            {
                await QueueAsync(NotificationChannel.Email, user.Email).ConfigureAwait(false);
            }
        }

        async Task QueueAsync(NotificationChannel channel, string? recipientEmail)
        {
            var result = await notifications.SendAsync(user.Id, NotificationType.Security,
                "A session was signed out",
                message,
                channel, tenantId: null, actionUrl: SessionsReviewPath,
                priority: NotificationPriority.Normal,
                referenceEntityId: @event.EventId, referenceEntityType: nameof(SessionEvictionDetectedV1),
                recipientEmail: recipientEmail, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result is null || result.IsFailure)
            {
                throw new InvalidOperationException("The session-eviction notification was not durably queued.");
            }
        }
    }
}
