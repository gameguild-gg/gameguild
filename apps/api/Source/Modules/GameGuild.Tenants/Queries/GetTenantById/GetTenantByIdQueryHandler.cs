using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Handler for getting a tenant by ID
/// </summary>
public class GetTenantByIdQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantByIdQuery, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken) { return await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken); }
}
