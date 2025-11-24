using GameGuild.CQRS;
using GameGuild.Users.Repositories;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for marking multiple notifications as read
/// </summary>
public class BulkMarkNotificationsAsReadCommandHandler(IUserNotificationRepository notificationRepository)
    : ICommandHandler<BulkMarkNotificationsAsReadCommand>
{
    public async Task<Unit> Handle(BulkMarkNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.NotificationIds == null || request.NotificationIds.Count == 0)
        {
            return Unit.Value; // Nothing to do
        }

        // Get notifications by IDs
        var notifications = await notificationRepository.GetByIdsAsync(request.UserId, request.NotificationIds, cancellationToken).ConfigureAwait(false);

        // Mark each as read
        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
            await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        await notificationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
