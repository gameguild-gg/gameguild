using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Handler for getting resource usage records
/// </summary>
public class GetResourceUsageRecordsQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetResourceUsageRecordsQuery, IEnumerable<UsageRecord>>
{
    public async Task<IEnumerable<UsageRecord>> Handle(GetResourceUsageRecordsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ResourceUsageType.HasValue)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                return await usageRecordRepository.GetByDateRangeAsync(request.TenantId, request.ResourceUsageType.Value, request.StartDate.Value, request.EndDate.Value, cancellationToken).ConfigureAwait(false);
            }

            return await usageRecordRepository.GetByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType.Value, cancellationToken).ConfigureAwait(false);
        }

        return await usageRecordRepository.GetByTenantAsync(request.TenantId, null, request.StartDate, request.EndDate, cancellationToken).ConfigureAwait(false);
    }
}
