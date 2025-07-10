using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get deleted tenants
/// </summary>
public class GetDeletedTenantsQuery : IQuery<Common.Result<IEnumerable<Tenant>>>
{
  // No additional parameters needed for this query
}
