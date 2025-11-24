using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk updating users
/// </summary>
public class BulkUpdateUsersCommandHandler(IUserRepository userRepository) : ICommandHandler<BulkUpdateUsersCommand>
{
    public async Task<Unit> Handle(BulkUpdateUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userIds = request.Updates.Select(u => u.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        var userDict = users.ToDictionary(u => u.Id);

        foreach (var updateItem in request.Updates)
        {
            if (!userDict.TryGetValue(updateItem.UserId, out var user)) continue;

            try
            {
                // Apply updates based on what's provided in the update item
                if (!string.IsNullOrWhiteSpace(updateItem.Name))
                {
                    user.UpdateName(updateItem.Name);
                }

                if (updateItem.PhoneNumber != null)
                {
                    user.UpdatePhoneNumber(updateItem.PhoneNumber);
                }

                await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Continue with other users even if one fails
                continue;
            }
        }

        return Unit.Value;
    }
}
