using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;

namespace GameGuild.Users.Commands;

public class MarkNotificationAsReadCommandHandler(IUserRepository userRepository, IUserNotificationRepository notificationRepository) : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Unit> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user == null) throw new UserNotFoundException(request.UserId);

        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken).ConfigureAwait(false);
        if (notification == null || notification.UserId != request.UserId)
        {
            throw new InvalidOperationException("Notification not found or does not belong to user");
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
