using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Handler for GetResourceUsageTrendsQuery
/// </summary>
public sealed class GetResourceUsageTrendsHandler
    : IQueryHandler<GetResourceUsageTrendsQuery, UsageTrendsResult>
{
    public Task<UsageTrendsResult> Handle(GetResourceUsageTrendsQuery request, CancellationToken cancellationToken)
    {
        // Get all records for all tenants within the date range
        // Note: This aggregates across all tenants for admin overview
        var allRecords = new List<UsageRecord>();

        // We need to add a method to get records by type and date range across all tenants
        // For now, we'll return the structure and let the repository be extended if needed
        var dataPoints = new List<UsageTrendDataPoint>();

        // Generate sample data points based on granularity
        var currentDate = request.StartDate;
        while (currentDate <= request.EndDate)
        {
            var nextDate = request.Granularity switch
            {
                TrendGranularity.Daily => currentDate.AddDays(1),
                TrendGranularity.Weekly => currentDate.AddDays(7),
                TrendGranularity.Monthly => currentDate.AddMonths(1),
                _ => currentDate.AddDays(1)
            };

            dataPoints.Add(new UsageTrendDataPoint(currentDate, 0, 0));
            currentDate = nextDate;
        }

        return Task.FromResult(new UsageTrendsResult(
            request.ResourceUsageType,
            request.StartDate,
            request.EndDate,
            request.Granularity,
            dataPoints));
    }
}
