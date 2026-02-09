using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for archiving multiple notifications
/// </summary>
public sealed class BulkArchiveNotificationsCommandHandler(IUserNotificationRepository notificationRepository)
    : ICommandHandler<BulkArchiveNotificationsCommand>
{
    public async Task<Unit> Handle(BulkArchiveNotificationsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.NotificationIds.Count == 0)
        {
            return Unit.Value; // Nothing to do
        }

        // Get notifications by IDs
        var notifications = await notificationRepository.GetByIdsAsync(request.UserId, request.NotificationIds, cancellationToken).ConfigureAwait(false);

        // Archive each
        foreach (var notification in notifications)
        {
            notification.Archive();
            await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        await notificationRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
