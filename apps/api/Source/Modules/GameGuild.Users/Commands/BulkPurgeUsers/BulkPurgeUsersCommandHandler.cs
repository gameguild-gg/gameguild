using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Entities;
using GameGuild.Users.Events;
using GameGuild.Users.Models;

namespace GameGuild.Users.Commands;

/// <summary>
///     Command handler for bulk permanently deleting (purging) users
/// </summary>
public class BulkPurgeUsersCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<BulkPurgeUsersCommand>
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

                // TODO: Implement strategy-based purging logic
                // - Immediate: Delete right away
                // - Scheduled: Schedule for future deletion
                // - GracePeriod: Mark for deletion after grace period

                // TODO: Implement hard delete functionality in repository
                // For now, use soft delete - need to add PurgeAsync method to IUserRepository
                await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);

                // Publish domain event
                await publisher.Publish(new UserPurgedNotification(userId, userEmail, userName, strategy), cancellationToken).ConfigureAwait(false);
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
