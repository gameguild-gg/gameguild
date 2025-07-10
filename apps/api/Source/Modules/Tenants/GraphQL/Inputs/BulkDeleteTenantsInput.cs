namespace GameGuild.Modules.Tenants.Inputs;

/// <summary>
/// Input for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsInput {
  public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}