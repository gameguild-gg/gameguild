using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for getting current user resource usage summary
/// </summary>
public sealed class GetCurrentUserResourceUsageSummaryQueryHandler(IUsageRecordRepository usageRecordRepository) : IQueryHandler<GetCurrentUserResourceUsageSummaryQuery, Dictionary<ResourceUsageType, long>>
{
    public async Task<Dictionary<ResourceUsageType, long>> Handle(GetCurrentUserResourceUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var summary = new Dictionary<ResourceUsageType, long>();

        foreach (ResourceUsageType type in Enum.GetValues<ResourceUsageType>())
        {
            var usage = await usageRecordRepository.GetCurrentUserUsageAsync(request.UserId, type, cancellationToken).ConfigureAwait(false);

            if (usage > 0) { summary[type] = usage; }
        }

        return summary;
    }
}
