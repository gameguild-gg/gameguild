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
    IApplicationDbContext dbContext,
    GameGuild.CQRS.IPublisher? publisher = null) : ICommandHandler<CreateTenantCommand, Guid>
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

        await PublishTenantCreatedAsync(tenant, request.AdminEmail, cancellationToken).ConfigureAwait(false);

        return tenant.Id;
    }

    /// <summary>
    ///     Issue #1184 G4: TenantCreatedEvent was declared but never raised; tenant creation
    ///     is its producing site (provisioning consumers react to the new tenant + owner).
    ///     Separate method so the publish stays outside the persistence method (GGARCH003).
    /// </summary>
    private async Task PublishTenantCreatedAsync(Tenant tenant, string adminEmail, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new TenantCreatedEvent(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            (GameGuild.EmailAddress)adminEmail), cancellationToken).ConfigureAwait(false);
    }
}
