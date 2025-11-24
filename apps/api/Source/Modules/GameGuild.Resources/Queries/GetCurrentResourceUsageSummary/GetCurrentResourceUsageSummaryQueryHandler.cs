using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Handler for getting current resource usage summary for a tenant
/// </summary>
public class GetCurrentResourceUsageSummaryQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetCurrentResourceUsageSummaryQuery, Dictionary<ResourceUsageType, int>>
{
    public async Task<Dictionary<ResourceUsageType, int>> Handle(GetCurrentResourceUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get all usage records for the tenant (current period logic would need to be added based on business rules)
        var usageRecords = await usageRecordRepository.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        // Group by usage type and sum the counts
        var result = usageRecords.GroupBy(ur => ur.Type).ToDictionary(g => g.Key, g => (int) g.Sum(ur => ur.Count));

        return result;
    }
}
