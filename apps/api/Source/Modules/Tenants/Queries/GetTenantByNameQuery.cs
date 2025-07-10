using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get a tenant by name
/// </summary>
public class GetTenantByNameQuery : IQuery<Common.Result<Tenant?>>
{
  public string Name { get; init; } = string.Empty;
  public bool IncludeDeleted { get; init; }

  public GetTenantByNameQuery(string name, bool includeDeleted = false)
  {
    Name = name;
    IncludeDeleted = includeDeleted;
  }
}
