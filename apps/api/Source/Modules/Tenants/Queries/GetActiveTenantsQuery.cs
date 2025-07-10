using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get active tenants
/// </summary>
public class GetActiveTenantsQuery : IQuery<Common.Result<IEnumerable<Tenant>>> {
  // Only gets active tenants, no additional parameters needed
}
