namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for creating a tenant
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
  /// Slug for the tenant (URL-friendly unique identifier)
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Slug { get; set; } = string.Empty;

  /// <summary>
  /// Whether this tenant should be active upon creation
  /// </summary>
  public bool IsActive { get; set; } = true;
}
