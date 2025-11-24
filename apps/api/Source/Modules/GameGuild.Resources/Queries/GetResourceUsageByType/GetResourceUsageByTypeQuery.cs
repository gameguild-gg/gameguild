using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to get resource usage by type across all tenants
/// </summary>
/// <param name="ResourceUsageType">Usage type</param>
/// <param name="StartDate">Start date for aggregation</param>
/// <param name="EndDate">End date for aggregation</param>
public record GetResourceUsageByTypeQuery(ResourceUsageType ResourceUsageType, DateTime StartDate, DateTime EndDate) : IQuery<Dictionary<Guid, int>>;
