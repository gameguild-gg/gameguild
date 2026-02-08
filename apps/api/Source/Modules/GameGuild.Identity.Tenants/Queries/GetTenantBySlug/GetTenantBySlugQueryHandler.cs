using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting a tenant by slug
/// </summary>
public class GetTenantBySlugQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantBySlugQuery, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenantBySlugQuery request, CancellationToken cancellationToken) { return await tenantRepository.GetBySlugAsync(request.Slug, cancellationToken).ConfigureAwait(false); }
}
