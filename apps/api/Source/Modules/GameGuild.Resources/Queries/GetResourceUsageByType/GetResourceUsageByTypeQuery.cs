using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get resource usage by type across all tenants
/// </summary>
/// <param name="ResourceUsageType">Usage type</param>
/// <param name="StartDate">Start date for aggregation</param>
/// <param name="EndDate">End date for aggregation</param>
public sealed record GetResourceUsageByTypeQuery(ResourceUsageType ResourceUsageType, DateTime StartDate, DateTime EndDate) : IQuery<Dictionary<Guid, int>>;
