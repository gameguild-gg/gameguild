namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant user group membership response
/// </summary>
public class TenantUserGroupMembershipDto {
  /// <summary>
  /// Membership ID
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// User ID
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// Group ID
  /// </summary>
  public Guid GroupId { get; set; }

  /// <summary>
  /// Whether this membership was auto-assigned
  /// </summary>
  public bool IsAutoAssigned { get; set; }

  /// <summary>
  /// When the membership was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When the membership was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Create from membership model
  /// </summary>
  public static TenantUserGroupMembershipDto FromTenantUserGroupMembership(TenantUserGroupMembership membership) {
    return new TenantUserGroupMembershipDto {
      Id = membership.Id,
      UserId = membership.UserId,
      GroupId = membership.UserGroupId,
      IsAutoAssigned = membership.IsAutoAssigned,
      CreatedAt = membership.CreatedAt,
      UpdatedAt = membership.UpdatedAt,
    };
  }
}
