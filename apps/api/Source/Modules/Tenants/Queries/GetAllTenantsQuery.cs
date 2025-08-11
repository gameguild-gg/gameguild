using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get all tenants
/// </summary>
public class GetAllTenantsQuery(bool includeDeleted = false) : IQuery<Common.Result<IEnumerable<Tenant>>> {
  public bool IncludeDeleted { get; init; } = includeDeleted;
}
