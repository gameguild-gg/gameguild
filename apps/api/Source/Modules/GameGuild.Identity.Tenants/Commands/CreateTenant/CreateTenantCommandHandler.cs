using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for creating tenant command
/// </summary>
public class CreateTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<CreateTenantCommand, Guid>
{
    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        // Validate slug uniqueness
        var isSlugUnique = await tenantRepository.IsSlugUniqueAsync(request.Slug, cancellationToken : cancellationToken).ConfigureAwait(false);

        if (!isSlugUnique) { throw new InvalidOperationException($"Slug '{request.Slug}' is already in use."); }

        // Create new tenant entity
        var tenant = new Tenant { Name = request.Name, Slug = request.Slug, AdminEmail = request.AdminEmail, Description = request.Description, IsActive = true };

        // Save to repository
        var createdTenant = await tenantRepository.CreateAsync(tenant, cancellationToken).ConfigureAwait(false);

        return createdTenant.Id;
    }
}
