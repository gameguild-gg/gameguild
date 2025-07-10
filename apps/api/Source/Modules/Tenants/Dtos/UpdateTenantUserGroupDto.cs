using System.ComponentModel.DataAnnotations;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for updating an existing tenant user group
/// </summary>
public class UpdateTenantUserGroupDto {
  /// <summary>
  /// Name of the user group
  /// </summary>
  [MaxLength(100)]
  public string? Name { get; set; }

  /// <summary>
  /// Description of what this group represents
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the parent group (for nested group hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool? IsActive { get; set; }

  /// <summary>
  /// Apply updates to user group model
  /// </summary>
  public void UpdateTenantUserGroup(TenantUserGroup userGroup) {
    if (!string.IsNullOrEmpty(Name)) userGroup.Name = Name;

    if (Description != null) userGroup.Description = Description;

    if (ParentGroupId.HasValue) userGroup.ParentGroupId = ParentGroupId.Value;

    if (IsActive.HasValue) userGroup.IsActive = IsActive.Value;
  }
}
