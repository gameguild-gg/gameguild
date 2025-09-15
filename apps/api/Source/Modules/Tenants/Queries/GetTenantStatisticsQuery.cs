using GameGuild.CQRS;
﻿using GameGuild;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Query to get tenant statistics
/// </summary>
public class GetTenantStatisticsQuery : IQuery<Result<TenantStatistics>> {
  // No additional parameters needed for this query
}
