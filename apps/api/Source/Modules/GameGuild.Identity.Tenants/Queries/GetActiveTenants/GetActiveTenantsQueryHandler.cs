using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting all active tenants
/// </summary>
public class GetActiveTenantsQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetActiveTenantsQuery, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetActiveTenantsQuery request, CancellationToken cancellationToken) { return await tenantRepository.GetActiveTenantsAsync(cancellationToken); }
}
