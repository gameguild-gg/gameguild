using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for marking multiple notifications as unread
/// </summary>
public sealed class BulkMarkNotificationsAsUnreadCommandHandler(IUserNotificationRepository notificationRepository)
    : ICommandHandler<BulkMarkNotificationsAsUnreadCommand>
{
    public async Task<Unit> Handle(BulkMarkNotificationsAsUnreadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.NotificationIds.Count == 0)
        {
            return Unit.Value; // Nothing to do
        }

        // Get notifications by IDs
        var notifications = await notificationRepository.GetByIdsAsync(request.UserId, request.NotificationIds, cancellationToken).ConfigureAwait(false);

        // Mark each as unread
        foreach (var notification in notifications)
        {
            notification.MarkAsUnread();
            await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        await notificationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
