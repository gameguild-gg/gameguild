using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;

namespace GameGuild.API.Core.Security;

/// <summary>
///     Owner confirmation for the termination of every other session after a password
///     change or reset (issue #1091), consuming the <see cref="OtherSessionsTerminatedV1"/>
///     durable event. Queues InApp always and Email when the user's delivery preferences
///     allow it, mirroring <see cref="SuspiciousLoginAlertHandler"/>. Gated by
///     <c>Authentication:SecurityNotifications:Enabled</c> (default on).
/// </summary>
internal sealed class SessionsTerminatedAlertHandler(
    IUserRepository users,
    INotificationService notifications,
    INotificationPreferenceService preferences,
    IConfiguration configuration,
    ILogger<SessionsTerminatedAlertHandler> logger) : IIntegrationEventHandler<OtherSessionsTerminatedV1>
{
    private const string EnabledPath = "Authentication:SecurityNotifications:Enabled";

    public async Task HandleAsync(OtherSessionsTerminatedV1 @event, CancellationToken cancellationToken = default)
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

        var tenantId = @event.TenantId == DurableIntegrationEventTenants.Platform ? (Guid?)null : @event.TenantId;
        var message = @event.TerminatedSessionCount == 1
            ? "All other sessions were terminated because your password changed."
            : $"All other sessions ({@event.TerminatedSessionCount}) were terminated because your password changed.";
        const string title = "Other sessions were terminated";

        var inAppResult = await notifications.SendAsync(user.Id, NotificationType.Security,
            title, message,
            NotificationChannel.InApp, tenantId, priority: NotificationPriority.Normal,
            referenceEntityId: @event.EventId, referenceEntityType: nameof(OtherSessionsTerminatedV1),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (inAppResult is null || inAppResult.IsFailure)
        {
            throw new InvalidOperationException("The sessions-terminated notification was not durably queued.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        // Normal-priority routing: the termination itself was an authenticated account
        // action, so channel toggles, mutes, and quiet hours still apply.
        var decision = await preferences.DecideDeliveryAsync(
            user.Id, NotificationType.Security, NotificationChannel.Email, NotificationPriority.Normal, cancellationToken)
            .ConfigureAwait(false);
        if (decision.Action != NotificationDeliveryAction.Send)
        {
            return;
        }

        var result = await notifications.SendAsync(user.Id, NotificationType.Security,
            title, message,
            NotificationChannel.Email, tenantId, priority: NotificationPriority.Normal,
            referenceEntityId: @event.EventId, referenceEntityType: nameof(OtherSessionsTerminatedV1),
            recipientEmail: user.Email, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result is null || result.IsFailure)
        {
            throw new InvalidOperationException("The sessions-terminated notification was not durably queued.");
        }

        logger.LogInformation(
            "Queued sessions-terminated owner notification for user {UserId} ({SessionCount} sessions)",
            @event.UserId, @event.TerminatedSessionCount);
    }
}
