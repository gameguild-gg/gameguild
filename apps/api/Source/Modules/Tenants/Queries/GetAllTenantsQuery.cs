using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;

namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get all tenants
/// </summary>
public class GetAllTenantsQuery(bool includeDeleted = false) : IQuery<Common.Result<IEnumerable<Tenant>>> {
    public bool IncludeDeleted { get; init; } = includeDeleted;
}