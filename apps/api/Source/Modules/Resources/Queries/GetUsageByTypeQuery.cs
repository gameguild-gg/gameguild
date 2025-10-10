using GameGuild.Messaging;

namespace GameGuild.Modules.Resources.Queries;

/// <summary>
/// Query to get usage aggregated by type across all tenants
/// </summary>
/// <param name="UsageType">Usage type to aggregate</param>
/// <param name="StartDate">Start date for aggregation</param>
/// <param name="EndDate">End date for aggregation</param>
public record GetUsageByTypeQuery(
    ResourceUsageType UsageType,
    DateTime StartDate,
    DateTime EndDate) : IRequest<Result<Dictionary<Guid, long>>>;
