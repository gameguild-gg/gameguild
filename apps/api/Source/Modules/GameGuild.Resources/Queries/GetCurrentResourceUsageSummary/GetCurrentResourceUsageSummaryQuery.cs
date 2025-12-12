using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to get current resource usage summary for a tenant
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
public record GetCurrentResourceUsageSummaryQuery(Guid TenantId) : IQuery<Dictionary<ResourceUsageType, int>>;
