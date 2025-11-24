using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk unsuspending users
/// </summary>
public class BulkUnsuspendUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkUnsuspendUsersCommand, BulkUnsuspendUsersResult>
{
    public async Task<BulkUnsuspendUsersResult> Handle(BulkUnsuspendUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);
        var unsuspendedUsers = new List<UserDto>();
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
                // Unsuspend by activating the user
                user.Activate();
                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                unsuspendedUsers.Add(new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt));
            }
            catch { failedUserIds.Add(userId); }
        }

        return new BulkUnsuspendUsersResult(unsuspendedUsers, failedUserIds);
    }
}
