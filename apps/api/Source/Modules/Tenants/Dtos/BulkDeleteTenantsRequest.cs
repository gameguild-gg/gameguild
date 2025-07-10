using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Request DTO for bulk deleting tenants
/// </summary>
public class BulkDeleteTenantsRequest
{
  [Required]
  public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}