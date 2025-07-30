namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant user group information
/// </summary>
public class TenantUserGroupDto {
  /// <summary>
  /// Unique identifier for the user group
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Name of the user group
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Description of the user group
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// ID of the parent group (for nested hierarchy)
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// Whether this group is currently active
  /// </summary>
  public bool IsActive { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool IsDefault { get; set; }

  /// <summary>
  /// When this group was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When this group was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Creates a DTO from a TenantUserGroup entity
  /// </summary>
  public static TenantUserGroupDto FromTenantUserGroup(TenantUserGroup userGroup) {
    return new TenantUserGroupDto {
      Id = userGroup.Id,
      Name = userGroup.Name,
      Description = userGroup.Description,
      TenantId = userGroup.TenantId,
      ParentGroupId = userGroup.ParentGroupId,
      IsActive = userGroup.IsActive,
      IsDefault = userGroup.IsDefault,
      CreatedAt = userGroup.CreatedAt,
      UpdatedAt = userGroup.UpdatedAt,
    };
  }
}
