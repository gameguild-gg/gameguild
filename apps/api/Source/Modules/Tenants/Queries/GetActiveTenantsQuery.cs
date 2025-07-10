using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants.Queries;

/// <summary>
/// Query to get active tenants
/// </summary>
public class GetActiveTenantsQuery : IQuery<Common.Result<IEnumerable<Tenant>>>
{
  // No additional parameters needed for this query
}
