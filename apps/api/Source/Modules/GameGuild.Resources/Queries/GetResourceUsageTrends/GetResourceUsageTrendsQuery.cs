using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get resource usage trends over time
/// </summary>
/// <param name="ResourceUsageType">Usage type to query</param>
/// <param name="StartDate">Start date for the trend period</param>
/// <param name="EndDate">End date for the trend period</param>
/// <param name="Granularity">Time granularity for aggregation</param>
public record GetResourceUsageTrendsQuery(
    ResourceUsageType ResourceUsageType,
    DateTime StartDate,
    DateTime EndDate,
    TrendGranularity Granularity) : IQuery<UsageTrendsResult>;
