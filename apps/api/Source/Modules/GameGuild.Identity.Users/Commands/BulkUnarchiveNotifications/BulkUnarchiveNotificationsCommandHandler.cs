using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for unarchiving multiple notifications
/// </summary>
public class BulkUnarchiveNotificationsCommandHandler(IUserNotificationRepository notificationRepository)
    : ICommandHandler<BulkUnarchiveNotificationsCommand>
{
    public async Task<Unit> Handle(BulkUnarchiveNotificationsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.NotificationIds.Count == 0)
        {
            return Unit.Value; // Nothing to do
        }

        // Get notifications by IDs
        var notifications = await notificationRepository.GetByIdsAsync(request.UserId, request.NotificationIds, cancellationToken).ConfigureAwait(false);

        // Unarchive each
        foreach (var notification in notifications)
        {
            notification.Unarchive();
            await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        await notificationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
