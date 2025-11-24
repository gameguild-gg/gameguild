using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to get quota information for a specific tenant and resource type
/// </summary>
/// <param name="TenantId">The tenant ID</param>
/// <param name="Type">The resource type</param>
public record GetResourceQuotaQuery(Guid TenantId, ResourceUsageType Type) : IQuery<ResourceQuotaResponse?>;
