using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for bulk restoring tenants
/// </summary>
public class BulkRestoreTenantsDto {
  /// <summary>
  /// List of tenant IDs to restore
  /// </summary>
  [Required]
  [MinLength(1, ErrorMessage = "At least one tenant ID must be provided")]
  public IEnumerable<Guid> TenantIds { get; set; } = new List<Guid>();
}
