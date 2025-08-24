namespace GameGuild.Modules.Tenants.Inputs;

/// <summary>
/// Input type for creating a new tenant
/// </summary>
public class CreateTenantInput {
  /// <summary>
  /// Name of the tenant
  /// </summary>
  [Required]
  [StringLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [StringLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;
}
