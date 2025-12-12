using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Handler for getting all active tenants
/// </summary>
public class GetActiveTenantsQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetActiveTenantsQuery, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetActiveTenantsQuery request, CancellationToken cancellationToken) { return await tenantRepository.GetActiveTenantsAsync(cancellationToken); }
}
