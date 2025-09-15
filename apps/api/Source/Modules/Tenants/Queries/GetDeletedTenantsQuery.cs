using GameGuild;
using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get deleted tenants
/// </summary>
public class GetDeletedTenantsQuery : IQuery<Result<IEnumerable<Tenant>>>
{
  // No additional parameters needed for this query
}
