using GameGuild.CQRS;
using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Queries;

/// <summary>
///     Query to get all active tenants
/// </summary>
public record GetActiveTenantsQuery : IQuery<IEnumerable<Tenant>>;
