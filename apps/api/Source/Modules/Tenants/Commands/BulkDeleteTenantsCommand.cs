using GameGuild.CQRS;
﻿using GameGuild;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to bulk delete multiple tenants
/// </summary>
public class BulkDeleteTenantsCommand(IEnumerable<Guid> tenantIds) : ICommand<Result<int>> {
  public IEnumerable<Guid> TenantIds { get; init; } = tenantIds ?? throw new ArgumentNullException(nameof(tenantIds));
}
