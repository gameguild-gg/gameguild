using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get a tenant by name
/// </summary>
public class GetTenantByNameQuery(string name, bool includeDeleted = false) : IQuery<Common.Result<Tenant?>> {
  public string Name { get; init; } = name;

  public bool IncludeDeleted { get; init; } = includeDeleted;
}
