namespace GameGuild.Modules.Tenants;

/// <summary>
/// DTO for tenant user group membership information
/// </summary>
public class TenantUserGroupMembershipDto {
  /// <summary>
  /// Unique identifier for the membership
  /// </summary>
  public Guid Id { get; set; }

  /// <summary>
  /// ID of the user
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// ID of the user group
  /// </summary>
  public Guid UserGroupId { get; set; }

  /// <summary>
  /// ID of the user group (alias for UserGroupId)
  /// </summary>
  public Guid GroupId { get; set; }

  /// <summary>
  /// When the user joined this group
  /// </summary>
  public DateTime JoinedAt { get; set; }

  /// <summary>
  /// Whether this membership was automatically assigned
  /// </summary>
  public bool IsAutoAssigned { get; set; }

  /// <summary>
  /// When this membership was created
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// When this membership was last updated
  /// </summary>
  public DateTime UpdatedAt { get; set; }

  /// <summary>
  /// Creates a DTO from a TenantUserGroupMembership entity
  /// </summary>
  public static TenantUserGroupMembershipDto FromTenantUserGroupMembership(TenantUserGroupMembership membership) {
    return new TenantUserGroupMembershipDto {
      Id = membership.Id,
      UserId = membership.UserId,
      UserGroupId = membership.UserGroupId,
      GroupId = membership.UserGroupId,
      JoinedAt = membership.JoinedAt,
      IsAutoAssigned = membership.IsAutoAssigned,
      CreatedAt = membership.CreatedAt,
      UpdatedAt = membership.UpdatedAt
    };
  }
}
