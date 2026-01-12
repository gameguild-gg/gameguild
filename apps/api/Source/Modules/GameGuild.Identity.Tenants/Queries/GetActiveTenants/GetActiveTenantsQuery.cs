using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Query to get all active tenants
/// </summary>
public record GetActiveTenantsQuery : IQuery<IEnumerable<Tenant>>;
