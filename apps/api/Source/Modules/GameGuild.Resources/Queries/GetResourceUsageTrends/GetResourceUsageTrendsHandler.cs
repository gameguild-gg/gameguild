using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for GetResourceUsageTrendsQuery
/// </summary>
public sealed class GetResourceUsageTrendsHandler(IUsageRecordRepository usageRecordRepository)
    : IQueryHandler<GetResourceUsageTrendsQuery, UsageTrendsResult>
{
    public async Task<UsageTrendsResult> Handle(GetResourceUsageTrendsQuery request, CancellationToken cancellationToken)
    {
        var dataPoints = new List<UsageTrendDataPoint>();

        if (request.StartDate > request.EndDate)
        {
            return new UsageTrendsResult(
                request.ResourceUsageType,
                request.StartDate,
                request.EndDate,
                request.Granularity,
                dataPoints);
        }

        var records = (await usageRecordRepository
                .GetByTypeAsync(request.ResourceUsageType, request.StartDate, request.EndDate, cancellationToken)
                .ConfigureAwait(false))
            .ToList();

        var currentDate = request.StartDate;
        while (currentDate <= request.EndDate)
        {
            var nextDate = GetNextPeriod(currentDate, request.Granularity);
            var bucketEnd = nextDate <= currentDate ? currentDate.AddDays(1) : nextDate;
            var bucketRecords = records
                .Where(record => record.PeriodStart >= currentDate && record.PeriodStart < bucketEnd)
                .ToList();

            dataPoints.Add(new UsageTrendDataPoint(
                currentDate,
                bucketRecords.Sum(record => record.Count),
                bucketRecords
                    .Where(record => record.TenantId.HasValue)
                    .Select(record => record.TenantId!.Value)
                    .Distinct()
                    .Count()));

            currentDate = bucketEnd;
        }

        return new UsageTrendsResult(
            request.ResourceUsageType,
            request.StartDate,
            request.EndDate,
            request.Granularity,
            dataPoints);
    }

    private static DateTime GetNextPeriod(DateTime currentDate, TrendGranularity granularity)
        => granularity switch
        {
            TrendGranularity.Daily => currentDate.AddDays(1),
            TrendGranularity.Weekly => currentDate.AddDays(7),
            TrendGranularity.Monthly => currentDate.AddMonths(1),
            _ => currentDate.AddDays(1)
        };
}
