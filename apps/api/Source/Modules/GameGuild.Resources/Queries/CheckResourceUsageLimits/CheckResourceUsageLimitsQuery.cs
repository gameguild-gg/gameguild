using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to check if tenant exceeds resource usage limits
/// </summary>
/// <param name="TenantId">Tenant unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
public sealed record CheckResourceUsageLimitsQuery(Guid TenantId, ResourceUsageType? ResourceUsageType = null) : IQuery<Dictionary<ResourceUsageType, bool>>;
