using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get all quotas for a specific tenant
/// </summary>
/// <param name="TenantId">The tenant ID to get quotas for</param>
public sealed record GetTenantResourceQuotasQuery(Guid TenantId) : IQuery<IEnumerable<ResourceQuotaResponse>>;
