using GameGuild.CQRS;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for permanently deleting (purging) users
/// </summary>
public class PurgeUserCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<PurgeUserCommand>
{
    public async Task<Unit> Handle(PurgeUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

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
        // that performs actual hard delete from database
        await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);

        // Publish domain event
        await publisher.Publish(new UserPurgedNotification(userId, userEmail, userName, strategy), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
