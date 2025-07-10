using System.ComponentModel.DataAnnotations;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Input type for updating an existing tenant
/// </summary>
public class UpdateTenantInput {
  /// <summary>
  /// ID of the tenant to update
  /// </summary>
  [Required]
  public Guid Id { get; set; }

  /// <summary>
  /// Name of the tenant
  /// </summary>
  [StringLength(100)]
  public string? Name { get; set; }

  /// <summary>
  /// Description of the tenant
  /// </summary>
  [StringLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// Whether this tenant is currently active
  /// </summary>
  public bool? IsActive { get; set; }
}
