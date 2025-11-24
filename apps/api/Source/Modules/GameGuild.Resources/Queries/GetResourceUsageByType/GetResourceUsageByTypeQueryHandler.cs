using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Handler for getting resource usage by type across all tenants
/// </summary>
public class GetResourceUsageByTypeQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetResourceUsageByTypeQuery, Dictionary<Guid, int>>
{
    public Task<Dictionary<Guid, int>> Handle(GetResourceUsageByTypeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // This query requires aggregating across all tenants, which may require a specialized repository method
        // For now, return an empty dictionary as this would typically require a database-wide query
        // In a real implementation, you might add a method like GetUsageByTypeAcrossTenantsAsync to the repository

        // TODO: Implement GetUsageByTypeAcrossTenantsAsync in IUsageRecordRepository for cross-tenant aggregation
        return Task.FromResult(new Dictionary<Guid, int>());
    }
}
