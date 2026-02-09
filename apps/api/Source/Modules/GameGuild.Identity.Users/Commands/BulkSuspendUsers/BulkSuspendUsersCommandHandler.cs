using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk suspending users
/// </summary>
public sealed class BulkSuspendUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkSuspendUsersCommand, BulkSuspendUsersResponse>
{
    public async Task<BulkSuspendUsersResponse> Handle(BulkSuspendUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = (await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false)).ToList();
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

        return new BulkSuspendUsersResponse(suspendedUsers, failedUserIds);
    }
}
