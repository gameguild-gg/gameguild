using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using UserModel = GameGuild.Modules.User.Models.User;


namespace GameGuild.Modules.Tenant.Models;

/// <summary>
/// Represents a user's membership in a tenant user group
/// </summary>
[Table("TenantUserGroupMemberships")]
[Index(nameof(UserId), nameof(UserGroupId), IsUnique = true)]
public class TenantUserGroupMembership : BaseEntity {
  private Guid _userId;
  private Guid _userGroupId;
  private DateTime _joinedAt = DateTime.UtcNow;
  private bool _isAutoAssigned = false;
  private UserModel _user = null!;
  private TenantUserGroup _userGroup = null!;

  /// <summary>
  /// ID of the user
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// ID of the user group
  /// </summary>
  [Required]
  public Guid UserGroupId {
    get => _userGroupId;
    set => _userGroupId = value;
  }

  /// <summary>
  /// When the user joined this group
  /// </summary>
  [Required]
  public DateTime JoinedAt {
    get => _joinedAt;
    set => _joinedAt = value;
  }

  /// <summary>
  /// Whether this membership was automatically assigned based on domain matching
  /// </summary>
  public bool IsAutoAssigned {
    get => _isAutoAssigned;
    set => _isAutoAssigned = value;
  }

  /// <summary>
  /// Navigation property to the user
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual UserModel User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Navigation property to the user group
  /// </summary>
  [ForeignKey(nameof(UserGroupId))]
  public virtual TenantUserGroup UserGroup {
    get => _userGroup;
    set => _userGroup = value;
  }

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
