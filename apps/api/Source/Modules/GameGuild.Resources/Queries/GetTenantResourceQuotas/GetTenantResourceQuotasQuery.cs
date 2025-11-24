using GameGuild.CQRS;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Queries;

/// <summary>
///     Query to get all quotas for a specific tenant
/// </summary>
/// <param name="TenantId">The tenant ID to get quotas for</param>
public record GetTenantResourceQuotasQuery(Guid TenantId) : IQuery<IEnumerable<ResourceQuotaResponse>>;
