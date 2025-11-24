using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk deactivating users
/// </summary>
public class BulkDeactivateUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkDeactivateUsersCommand, BulkDeactivateUsersResult>
{
    public async Task<BulkDeactivateUsersResult> Handle(BulkDeactivateUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);
        var deactivatedUsers = new List<UserDto>();
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
                user.Deactivate();
                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

                deactivatedUsers.Add(new UserDto(user.Id, user.Email, user.Name, user.CreatedAt, user.UpdatedAt, user.IsActive, user.PhoneNumber, user.LastSeenAt));
            }
            catch { failedUserIds.Add(userId); }
        }

        return new BulkDeactivateUsersResult(deactivatedUsers, failedUserIds);
    }
}
