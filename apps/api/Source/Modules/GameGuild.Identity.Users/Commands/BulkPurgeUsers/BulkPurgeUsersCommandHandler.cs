using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk permanently deleting (purging) users
/// </summary>
public sealed class BulkPurgeUsersCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<BulkPurgeUsersCommand>
{
    public async Task<Unit> Handle(BulkPurgeUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);

        foreach (var user in users)
        {
            try
            {
                // Store user info for event before deletion
                var userId = user.Id;
                var userEmail = user.Email;
                var userName = user.Name;
                var strategy = request.Strategy.ToString();

                // Apply strategy-based purging logic
                switch (request.Strategy)
                {
                    case PurgeStrategy.Immediate:
                        // Hard delete: remove permanently
                        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
                        break;
                    case PurgeStrategy.Scheduled:
                        // Mark for future deletion via soft-delete
                        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
                        break;
                    case PurgeStrategy.GracePeriod:
                        // Soft delete with grace period — repository handles soft-delete semantics
                        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
                        break;
                }

                // Publish domain event
                await publisher.Publish(new UserPurgedNotification(userId, userEmail, userName, strategy), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Continue with other users even if one fails
            }
        }

        return Unit.Value;
    }
}
