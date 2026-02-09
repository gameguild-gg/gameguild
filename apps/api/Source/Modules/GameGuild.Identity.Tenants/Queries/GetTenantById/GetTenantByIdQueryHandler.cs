using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting a tenant by ID
/// </summary>
public sealed class GetTenantByIdQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantByIdQuery, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken) { return await tenantRepository.GetByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false); }
}
