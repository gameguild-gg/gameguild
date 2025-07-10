using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request DTO for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsRequest
{
  [Required]
  public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}