using GameGuild.Common;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Command to bulk restore multiple tenants
/// </summary>
public class BulkRestoreTenantsCommand(IEnumerable<Guid> tenantIds) : ICommand<Common.Result<int>> {
  public IEnumerable<Guid> TenantIds { get; init; } = tenantIds ?? throw new ArgumentNullException(nameof(tenantIds));
}