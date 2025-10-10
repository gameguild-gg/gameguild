using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Query to get all archived tenants
/// </summary>
public record GetArchivedTenantsQuery : IQuery<IEnumerable<Tenant>>;