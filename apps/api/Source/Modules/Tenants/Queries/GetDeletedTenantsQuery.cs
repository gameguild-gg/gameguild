using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get deleted tenants
/// </summary>
public class GetDeletedTenantsQuery : IQuery<Common.Result<IEnumerable<Tenant>>> {
  // No additional parameters needed for this query
}
