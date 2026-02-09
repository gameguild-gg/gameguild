using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk activating users
/// </summary>
public sealed class BulkActivateUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkActivateUsersCommand, BulkActivateUsersResponse>
{
    public async Task<BulkActivateUsersResponse> Handle(BulkActivateUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = (await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false)).ToList();
        var activatedUsers = new List<UserDto>();
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
                user.Activate();
                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                activatedUsers.Add(new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt));
            }
            catch { failedUserIds.Add(userId); }
        }

        return new BulkActivateUsersResponse(activatedUsers, failedUserIds);
    }
}
