using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for deleting users (soft delete).
///     Decrements the Users quota to maintain accurate resource accounting.
/// </summary>
public class DeleteUserCommandHandler(
    IUserRepository userRepository,
    IPublisher publisher,
    IResourceQuotaService quotaService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<DeleteUserCommand>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false) 
            ?? throw new UserNotFoundException($"User with ID {request.UserId} not found");

        // Use domain method for soft delete
        user.MarkDeleted();
        await userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        // Decrement quota to maintain accurate resource accounting
        if (Actor.TenantId.HasValue)
        {
            // Extract user GUID from SubjectId if available
            Guid? actorUserId = Guid.TryParse(Actor.SubjectId, out var parsedId) ? parsedId : null;

            await quotaService.DecrementUsageAsync(
                Actor.TenantId.Value,
                ResourceUsageType.Users,
                1,
                actorUserId,
                "DeleteUser",
                cancellationToken);
        }

        // Publish domain event
        await publisher.Publish(new UserDeletedNotification(user.Id), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
