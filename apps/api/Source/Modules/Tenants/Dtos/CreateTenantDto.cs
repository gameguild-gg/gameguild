using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for creating a new tenant
/// </summary>
public class CreateTenantDto {
  /// <summary>
  /// Name of the tenant
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;
}
