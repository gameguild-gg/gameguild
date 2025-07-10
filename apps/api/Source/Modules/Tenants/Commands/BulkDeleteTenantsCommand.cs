using GameGuild.Common;


namespace GameGuild.Modules.Tenants.Commands;

/// <summary>
/// Command to bulk delete multiple tenants
/// </summary>
public class BulkDeleteTenantsCommand(IEnumerable<Guid> tenantIds) : ICommand<Common.Result<int>> {
  public IEnumerable<Guid> TenantIds { get; init; } = tenantIds ?? throw new ArgumentNullException(nameof(tenantIds));
}