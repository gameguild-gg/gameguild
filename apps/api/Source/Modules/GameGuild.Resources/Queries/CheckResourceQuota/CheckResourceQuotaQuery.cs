using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to check if the quota allows the given resource consumption
/// </summary>
/// <param name="TenantId">The tenant ID to check quota for</param>
/// <param name="Type">The type of resource usage to check</param>
/// <param name="Amount">The amount of resource to consume (default is 1)</param>
public record CheckResourceQuotaQuery(Guid TenantId, ResourceUsageType Type, long Amount = 1) : IQuery<ResourceQuotaEnforcementResult>;
