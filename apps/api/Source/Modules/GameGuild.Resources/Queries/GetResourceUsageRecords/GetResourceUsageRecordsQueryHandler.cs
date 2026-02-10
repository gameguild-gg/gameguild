using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for getting resource usage records with pagination
/// </summary>
public sealed class GetResourceUsageRecordsQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetResourceUsageRecordsQuery, PagedResult<UsageRecord>>
{
    /// <summary>
    ///     Maximum allowed page size to prevent excessive memory usage
    /// </summary>
    private const int MaxPageSize = 200;
    
    public async Task<PagedResult<UsageRecord>> Handle(GetResourceUsageRecordsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        // Enforce page size limits
        var pageSize = Math.Min(Math.Max(1, request.PageSize), MaxPageSize);
        var pageNumber = Math.Max(1, request.PageNumber);
        var skip = (pageNumber - 1) * pageSize;

        return await usageRecordRepository.GetPagedByTenantAsync(
            request.TenantId,
            request.ResourceUsageType,
            request.StartDate,
            request.EndDate,
            skip,
            pageSize,
            cancellationToken).ConfigureAwait(false);
    }
}
