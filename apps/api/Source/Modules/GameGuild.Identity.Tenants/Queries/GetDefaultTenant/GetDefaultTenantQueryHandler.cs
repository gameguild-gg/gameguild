using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for getting the default tenant
/// </summary>
public sealed class GetDefaultTenantQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetDefaultTenantQuery, Tenant?>
{
    public async Task<Tenant?> Handle(GetDefaultTenantQuery request, CancellationToken cancellationToken)
    {
        var queryable = await tenantRepository.GetQueryableAsync(cancellationToken).ConfigureAwait(false);
        return await queryable
            .Where(t => t.IsDefault && t.IsActive)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }
}
