using GameGuild.CQRS;
using GameGuild.Resources;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Users;

/// <summary>
///     Command handler for bulk creating users with quota enforcement
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

        // Check quota before processing (if tenant context is available)
        if (Actor.TenantId.HasValue && userCount > 0)
        {
            var limitCheck = await quotaService.CheckLimitsAsync(
                Actor.TenantId.Value,
                ResourceUsageType.Users,
                userCount,
                cancellationToken).ConfigureAwait(false);

            if (!limitCheck.CanProceed)
            {
                throw new QuotaExceededException(
                    $"Cannot create {userCount} users. Quota exceeded.",
                    ResourceUsageType.Users,
                    limitCheck.CurrentUsage,
                    limitCheck.HardLimit ?? 0,
                    Actor.TenantId.Value);
            }
        }

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

        // Record usage after successful creation
        if (Actor.TenantId.HasValue && createdUserIds.Count > 0)
        {
            await quotaService.RecordUsageAsync(
                Actor.TenantId.Value,
                ResourceUsageType.Users,
                createdUserIds.Count,
                source: "BulkCreateUsers",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return new BulkCreateUsersResponse(createdUserIds, failedEmails);
    }
}
