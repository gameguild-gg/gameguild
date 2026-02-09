using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get current resource usage summary for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public sealed record GetCurrentResourceUsageSummaryQuery(Guid TenantId) : IQuery<Dictionary<ResourceUsageType, int>>;
