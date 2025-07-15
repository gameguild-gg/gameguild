using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get tenant statistics
/// </summary>
public class GetTenantStatisticsQuery : IQuery<Common.Result<TenantStatistics>> {
  // No additional parameters needed for this query
}
