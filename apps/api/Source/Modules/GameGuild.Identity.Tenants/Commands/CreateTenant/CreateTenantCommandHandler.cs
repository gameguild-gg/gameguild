using GameGuild;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for creating tenant command
/// </summary>
public sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IActorContextAccessor actorContextAccessor,
    IApplicationDbContext dbContext) : ICommandHandler<CreateTenantCommand, Guid>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorUserId = Actor.SubjectIdAsGuid
            ?? throw new AuthenticationRequiredException("Authenticated user ID is required to create a tenant.");

        // Validate slug uniqueness
        var isSlugUnique = await tenantRepository.IsSlugUniqueAsync(request.Slug, cancellationToken : cancellationToken).ConfigureAwait(false);

        if (!isSlugUnique) { throw new InvalidOperationException($"Slug '{request.Slug}' is already in use."); }

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug,
            AdminEmail = request.AdminEmail,
            Description = request.Description,
            IsActive = true
        };

        dbContext.Set<Tenant>().Add(tenant);
        dbContext.Set<TenantMember>().Add(
            new TenantMember
            {
                TenantId = tenant.Id,
                UserId = actorUserId,
                Role = TenantRole.Owner,
                JoinedAt = SystemClock.UtcNow,
                IsActive = true
            });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenant.Id;
    }
}
