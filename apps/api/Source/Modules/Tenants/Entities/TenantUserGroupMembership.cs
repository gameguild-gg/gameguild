using GameGuild.Common;
using UserModel = GameGuild.Modules.Users.User;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents a user's membership in a tenant user group
/// </summary>
[Table("TenantUserGroupMemberships")]
[Index(nameof(UserId), nameof(UserGroupId), IsUnique = true)]
public class TenantUserGroupMembership : Entity {
  /// <summary>
  /// ID of the user
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// ID of the user group
  /// </summary>
  [Required]
  public Guid UserGroupId { get; set; }

  /// <summary>
  /// When the user joined this group
  /// </summary>
  [Required]
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Whether this membership was automatically assigned based on domain matching
  /// </summary>
  public bool IsAutoAssigned { get; set; }

  /// <summary>
  /// Navigation property to the user
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual UserModel User { get; set; } = null!;

  /// <summary>
  /// Navigation property to the user group
  /// </summary>
  [ForeignKey(nameof(UserGroupId))]
  public virtual TenantUserGroup UserGroup { get; set; } = null!;

  /// <summary>
  /// Default constructor
  /// </summary>
  public TenantUserGroupMembership() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial membership data</param>
  public TenantUserGroupMembership(object partial) : base(partial) { }

  /// <summary>
  /// Constructor to create a new membership
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <param name="userGroupId">User group ID</param>
  /// <param name="isAutoAssigned">Whether this was automatically assigned</param>
  public TenantUserGroupMembership(Guid userId, Guid userGroupId, bool isAutoAssigned = false) {
    UserId = userId;
    UserGroupId = userGroupId;
    IsAutoAssigned = isAutoAssigned;
    JoinedAt = DateTime.UtcNow;
  }
}
