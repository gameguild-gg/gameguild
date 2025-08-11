namespace GameGuild.Modules.Tenants.Inputs;

/// <summary>
/// Input for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsInput {
  public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}
