using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to check if tenant exceeds resource usage limits
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
public record CheckResourceUsageLimitsQuery(Guid TenantId, ResourceUsageType? ResourceUsageType = null) : IQuery<Dictionary<ResourceUsageType, bool>>;
