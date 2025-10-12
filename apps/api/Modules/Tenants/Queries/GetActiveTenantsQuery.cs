using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Query to get active tenants </summary>
public class GetActiveTenantsQuery : IQuery<Result<IEnumerable<Tenant>>>
{
    // Only gets active tenants, no additional parameters needed
}
