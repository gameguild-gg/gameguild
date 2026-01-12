using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for permanently deleting (purging) users
/// </summary>
public class PurgeUserCommandHandler(IUserRepository userRepository, IPublisher publisher) : ICommandHandler<PurgeUserCommand>
{
    public async Task<Unit> Handle(PurgeUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Include tenant memberships for validation
        var user = await userRepository.GetQueryable()
            .IgnoreQueryFilters()
            .Include(u => u.TenantMemberships)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            .ConfigureAwait(false) 
            ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        // Validate using domain method (throws if constraints violated)
        user.ValidatePurge();

        // Store user info for event before deletion
        var userId = user.Id;
        var userEmail = user.Email;
        var userName = user.Name;
        var strategy = request.Strategy.ToString();

        // Perform hard delete
        await userRepository.PurgeAsync(user, cancellationToken).ConfigureAwait(false);

        // Publish domain event
        await publisher.Publish(new UserPurgedNotification(userId, userEmail, userName, strategy), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
