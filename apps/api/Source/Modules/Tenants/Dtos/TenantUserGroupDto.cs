namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant user group response
/// </summary>
public class TenantUserGroupDto {
  /// <summary>
  /// Group ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// Group name
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Group description
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Whether this is the default group for auto-assignment
  /// </summary>
  public bool IsDefault { get; set; }

  /// <summary>
  /// Parent group ID if this is a subgroup
  /// </summary>
  public Guid? ParentGroupId { get; set; }

  /// <summary>
  /// ID of the tenant this group belongs to
  /// </summary>
  public Guid TenantId { get; set; }

  /// <summary>
  /// Whether the group is active
  /// </summary>
  public bool IsActive { get; set; }

  /// <summary>
  /// When the group was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the group was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from group model
  /// </summary>
  public static TenantUserGroupDto FromTenantUserGroup(TenantUserGroup group) {
    return new TenantUserGroupDto {
      Id = group.Id,
      Name = group.Name,
      Description = group.Description,
      IsDefault = group.IsDefault,
      ParentGroupId = group.ParentGroupId,
      TenantId = group.TenantId,
      IsActive = group.IsActive,
      CreatedAt = group.CreatedAt,
      UpdatedAt = group.UpdatedAt,
    };
  }
}
