using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk deleting users.
///     Decrements the Users quota to maintain accurate resource accounting.
/// </summary>
public sealed class BulkDeleteUsersCommandHandler(
    IUserRepository userRepository,
    IResourceQuotaService quotaService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<BulkDeleteUsersCommand>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<Unit> Handle(BulkDeleteUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var users = await userRepository.GetByIdsAsync(request.UserIds, cancellationToken).ConfigureAwait(false);
        var deletedCount = 0;

        foreach (var user in users)
        {
            await userRepository.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
            deletedCount++;
        }

        // Decrement quota by the number of deleted users
        if (Actor.TenantId.HasValue && deletedCount > 0)
        {
            // Extract user GUID from SubjectId if available
            Guid? actorUserId = Guid.TryParse(Actor.SubjectId, out var parsedId) ? parsedId : null;

            await quotaService.DecrementUsageAsync(
                Actor.TenantId.Value,
                ResourceUsageType.Users,
                deletedCount,
                actorUserId,
                "BulkDeleteUsers",
                cancellationToken);
        }

        return Unit.Value;
    }
}
