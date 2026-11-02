using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Users;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using NotificationPriority = GameGuild.Notifications.NotificationPriority;

namespace GameGuild.API.Core.Security;

/// <summary>
///     Delivers in-app and email reminders to reviewers with pending access-review
///     items (issue #1204). Consumes the <see cref="AccessReviewReminderNotification"/>
///     published by <c>AccessReviewService.SendRemindersAsync</c> — previously an
///     orphan producer that incremented reminder counters while publishing into the
///     void — and routes it through the Notifications module, so the recipient's
///     per-channel preferences (drop/digest/send) are honored and the delivery goes
///     through the module's durable queue like every other outbound notification.
/// </summary>
/// <remarks>
///     <para>
///         Resides in the API composition root (same placement as
///         <see cref="PermissionExpirationAlertHandler"/>) because the publishing module
///         must stay platform-pure and cannot reference the Notifications delivery
///         stack. Registered by the CQRS assembly scan as an
///         <see cref="INotificationHandler{TNotification}"/>.
///     </para>
///     <para>
///         <b>Failure semantics:</b> queueing failures are logged, not thrown — the
///         reminder stamp was already persisted by the publisher, so rethrowing would
///         not trigger redelivery and would only surface as command noise (same
///         contract as <see cref="PermissionExpirationAlertHandler"/>).
///     </para>
/// </remarks>
internal sealed class AccessReviewReminderAlertHandler(
    IUserRepository users,
    INotificationService notifications,
    ILogger<AccessReviewReminderAlertHandler> logger) : INotificationHandler<AccessReviewReminderNotification>
{
    private const string PendingItemsActionUrl = "/access-reviews/items/pending";

    public async Task Handle(AccessReviewReminderNotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var user = await users.GetByIdAsync(notification.ReviewerId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            logger.LogDebug(
                "Access review reminder {ItemId} targets unknown or deleted reviewer {ReviewerId}; nothing to deliver.",
                notification.ItemId,
                notification.ReviewerId);
            return;
        }

        const string title = "Access review reminder";
        var message =
            "You have an access review item awaiting your decision. " +
            "Open your pending review items and approve or revoke the listed access before the campaign ends.";

        await QueueAsync(NotificationChannel.InApp, null).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await QueueAsync(NotificationChannel.Email, user.Email).ConfigureAwait(false);
        }

        async Task QueueAsync(NotificationChannel channel, string? recipientEmail)
        {
            var result = await notifications.SendAsync(
                user.Id,
                NotificationType.Security,
                title,
                message,
                channel,
                tenantId: null,
                actionUrl: PendingItemsActionUrl,
                priority: NotificationPriority.Normal,
                referenceEntityId: notification.ItemId,
                referenceEntityType: nameof(AccessReviewReminderNotification),
                recipientEmail: recipientEmail,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (result is null || result.IsFailure)
            {
                // Only public channel labels cross the log boundary; recipient details stay in the delivery call.
                var channelLabel = channel switch
                {
                    NotificationChannel.InApp => "in-app",
                    NotificationChannel.Email => "email",
                    _ => "other"
                };
                logger.LogWarning(
                    "Failed to queue access review reminder {ItemId} for campaign {CampaignId} on channel {Channel}.",
                    notification.ItemId,
                    notification.CampaignId,
                    channelLabel);
            }
        }
    }
}
