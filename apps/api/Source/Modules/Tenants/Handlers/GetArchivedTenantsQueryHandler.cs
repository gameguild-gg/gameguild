using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary>
///     Handler for getting archived tenants query
/// </summary>
public class GetArchivedTenantsQueryHandler(ITenantRepository tenantRepository, ILogger<GetArchivedTenantsQueryHandler> logger) : IQueryHandler<GetArchivedTenantsQuery, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetArchivedTenantsQuery request, CancellationToken cancellationToken)
    {
        logger.LogDebug("Retrieving archived tenants");

        var tenants = await tenantRepository.GetArchivedAsync(cancellationToken);

        logger.LogDebug("Retrieved {Count} archived tenants", tenants.Count());
        return tenants;
    }
}
