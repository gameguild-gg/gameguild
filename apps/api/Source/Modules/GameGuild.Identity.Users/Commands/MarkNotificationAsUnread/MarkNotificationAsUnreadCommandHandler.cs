using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

public sealed class MarkNotificationAsUnreadCommandHandler(IUserRepository userRepository, IUserNotificationRepository notificationRepository) : ICommandHandler<MarkNotificationAsUnreadCommand>
{
    public async Task<Unit> Handle(MarkNotificationAsUnreadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken).ConfigureAwait(false);
        if (notification == null || notification.UserId != request.UserId)
        {
            throw new InvalidOperationException("Notification not found or does not belong to user");
        }

        notification.MarkAsUnread();
        await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
