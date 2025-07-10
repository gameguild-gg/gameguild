using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get active tenants
/// </summary>
public class GetActiveTenantsQuery : IQuery<Common.Result<IEnumerable<Tenant>>> {
  // Only gets active tenants, no additional parameters needed
}
