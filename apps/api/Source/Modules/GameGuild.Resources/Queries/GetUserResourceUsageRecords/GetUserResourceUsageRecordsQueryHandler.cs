using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for getting user resource usage records
/// </summary>
public sealed class GetUserResourceUsageRecordsQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetUserResourceUsageRecordsQuery, IEnumerable<UsageRecord>>
{
    public async Task<IEnumerable<UsageRecord>> Handle(GetUserResourceUsageRecordsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ResourceUsageType.HasValue)
        {
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                return await usageRecordRepository.GetByUserDateRangeAsync(request.UserId, request.ResourceUsageType.Value, request.StartDate.Value, request.EndDate.Value, cancellationToken).ConfigureAwait(false);
            }

            return await usageRecordRepository.GetByUserAndTypeAsync(request.UserId, request.ResourceUsageType.Value, cancellationToken).ConfigureAwait(false);
        }

        return await usageRecordRepository.GetByUserAsync(request.UserId, null, request.StartDate, request.EndDate, cancellationToken).ConfigureAwait(false);
    }
}
