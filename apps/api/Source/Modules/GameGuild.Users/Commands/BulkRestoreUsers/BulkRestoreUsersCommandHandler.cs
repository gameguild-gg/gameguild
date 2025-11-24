using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Events;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk restoring soft-deleted users
/// </summary>
public class BulkRestoreUsersCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<BulkRestoreUsersCommand, BulkRestoreUsersResult>
{
    public async Task<BulkRestoreUsersResult> Handle(BulkRestoreUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);
        var restoredUsers = new List<UserDto>();
        var failedUserIds = new List<Guid>();

        foreach (var userId in request.UserIds)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                failedUserIds.Add(userId);

                continue;
            }

            try
            {
                user.Restore();
                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                // Publish domain event
                await publisher.Publish(new UserRestoredNotification(user.Id, user.Email, user.Name), cancellationToken).ConfigureAwait(false);

                restoredUsers.Add(new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt));
            }
            catch { failedUserIds.Add(userId); }
        }

        return new BulkRestoreUsersResult(restoredUsers, failedUserIds);
    }
}
