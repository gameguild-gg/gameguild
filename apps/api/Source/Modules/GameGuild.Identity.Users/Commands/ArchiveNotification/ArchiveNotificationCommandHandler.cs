using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed class ArchiveNotificationCommandHandler(IUserRepository userRepository, IUserNotificationRepository notificationRepository) : ICommandHandler<ArchiveNotificationCommand>
{
    public async Task<Unit> Handle(ArchiveNotificationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken).ConfigureAwait(false);
        if (notification == null || notification.UserId != request.UserId)
        {
            throw new InvalidOperationException("Notification not found or does not belong to user");
        }

        notification.Archive();
        await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
