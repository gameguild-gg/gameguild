using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for creating a new tenant user group
/// </summary>
public class CreateTenantUserGroupDto {
  /// <summary>
  /// Name of the user group (e.g., "Students", "Professors", "Administrators")
  /// </summary>
  [Required]
  [MaxLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of what this group represents
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  [Required]
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the parent group (for nested group hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool IsDefault { get; set; } = false;

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Convert DTO to domain model
  /// </summary>
  public TenantUserGroup ToTenantUserGroup() {
    return new TenantUserGroup {
      Name = Name,
      Description = Description,
      TenantId = TenantId,
      ParentGroupId = ParentGroupId,
      IsDefault = IsDefault,
      IsActive = IsActive,
    };
  }
}
