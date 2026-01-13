using GameGuild.CQRS;
using GameGuild.Resources;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk creating users with atomic quota enforcement.
///     Uses TryAtomicConsumeAsync to prevent race conditions under concurrent access.
/// </summary>
public class BulkCreateUsersCommandHandler(
    IUserRepository userRepository,
    IResourceQuotaService quotaService,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<BulkCreateUsersCommand, BulkCreateUsersResponse>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<BulkCreateUsersResponse> Handle(BulkCreateUsersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userCount = request.Users.Count();

        // Track whether quota was consumed (for rollback on failure)
        var quotaConsumed = false;
        var quotaAmount = 0;

        // ATOMIC quota enforcement (if tenant context is available)
        if (Actor.TenantId.HasValue && userCount > 0)
        {
            var (success, currentUsage, hardLimit) = await quotaService.TryAtomicConsumeAsync(
                Actor.TenantId.Value,
                ResourceUsageType.Users,
                userCount,
                cancellationToken).ConfigureAwait(false);

            if (!success)
            {
                throw new QuotaExceededException(
                    $"Cannot create {userCount} users. Quota exceeded. Current: {currentUsage}, Limit: {hardLimit}",
                    ResourceUsageType.Users,
                    currentUsage,
                    hardLimit ?? 0,
                    Actor.TenantId.Value);
            }

            quotaConsumed = true;
            quotaAmount = userCount;
        }

        try
        {
            var createdUserIds = new List<Guid>();
            var failedEmails = new List<string>();
            var usersToCreate = new List<User>();

            // Validate all emails don't already exist
            var emails = request.Users.Select(u => u.Email).ToList();
            var existingUsers = await userRepository.GetByEmailsAsync(emails, cancellationToken).ConfigureAwait(false);
            var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();

            foreach (var userRequest in request.Users)
            {
                if (existingEmails.Contains(userRequest.Email))
                {
                    failedEmails.Add(userRequest.Email);
                    continue;
                }

                try
                {
                    // Create new user
                    var user = User.Create(userRequest.Email, userRequest.Name, userRequest.PhoneNumber);
                    usersToCreate.Add(user);
                    createdUserIds.Add(user.Id);
                }
                catch { failedEmails.Add(userRequest.Email); }
            }

            // Add all users to repository
            foreach (var user in usersToCreate) { await userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false); }

            await userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Adjust quota if fewer users were created than requested
            var actualCreated = createdUserIds.Count;
            if (Actor.TenantId.HasValue && quotaConsumed && actualCreated < quotaAmount)
            {
                // Decrement the difference (we reserved for userCount but only created actualCreated)
                var difference = quotaAmount - actualCreated;
                await quotaService.DecrementUsageAsync(
                    Actor.TenantId.Value,
                    ResourceUsageType.Users,
                    difference,
                    source: "BulkCreateUsers:Adjustment",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return new BulkCreateUsersResponse(createdUserIds, failedEmails);
        }
        catch (Exception) when (quotaConsumed)
        {
            // Rollback quota on failure
            if (Actor.TenantId.HasValue)
            {
                await quotaService.DecrementUsageAsync(
                    Actor.TenantId.Value,
                    ResourceUsageType.Users,
                    quotaAmount,
                    source: "BulkCreateUsers:Rollback",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            throw;
        }
    }
}
