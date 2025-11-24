using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk suspending users
/// </summary>
public class BulkSuspendUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkSuspendUsersCommand, BulkSuspendUsersResult>
{
    public async Task<BulkSuspendUsersResult> Handle(BulkSuspendUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);
        var suspendedUsers = new List<UserDto>();
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
                // Suspend by deactivating the user
                user.Deactivate();
                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                suspendedUsers.Add(new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt));
            }
            catch { failedUserIds.Add(userId); }
        }

        return new BulkSuspendUsersResult(suspendedUsers, failedUserIds);
    }
}
