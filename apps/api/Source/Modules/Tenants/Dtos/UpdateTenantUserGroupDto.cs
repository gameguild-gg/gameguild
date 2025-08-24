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
  /// Description of the user group
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// ID of the parent group (for nested hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool? IsActive { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool? IsDefault { get; set; }
}
