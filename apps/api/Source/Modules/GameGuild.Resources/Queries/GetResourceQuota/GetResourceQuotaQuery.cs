using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get quota information for a specific tenant and resource type
/// </summary>
/// <param name="TenantId">The tenant ID</param>
/// <param name="Type">The resource type</param>
public sealed record GetResourceQuotaQuery(Guid TenantId, ResourceUsageType Type) : IQuery<ResourceQuotaResponse?>;
