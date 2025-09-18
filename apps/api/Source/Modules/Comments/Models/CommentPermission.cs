using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Comments;

/// <summary> Resource-specific permissions for Comment entities (Layer 3 of DAC permission system) Provides granular permission control for individual comments </summary>
[Table("CommentPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true, Name = "IX_CommentPermissions_User_Tenant_Resource")]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_CommentPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_CommentPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_CommentPermissions_Expiration")]
public class CommentPermission : ResourcePermission<Comment> {
  // Public parameterless constructor for EF and GraphQL
  public CommentPermission() { }

  // Public constructor for creating instances
  public CommentPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) { }
  // Comment-specific computed properties

  /// <summary> Check if user can edit this specific comment </summary>
  public bool CanEdit { get => HasPermission(PermissionType.Edit) && !IsExpired; }

  /// <summary> Check if user can reply to this specific comment </summary>
  public bool CanReply { get => HasPermission(PermissionType.Reply) && !IsExpired; }

  /// <summary> Check if user can moderate this specific comment </summary>
  public bool CanModerate { get => HasPermission(PermissionType.Review) && !IsExpired; }

  /// <summary> Check if user can delete this specific comment (owners always can delete) </summary>
  public bool CanDelete { get => HasPermission(PermissionType.Delete) && !IsExpired; }

  /// <summary> Check if user can flag this comment for review </summary>
  public bool CanFlag { get => HasPermission(PermissionType.Flag) && !IsExpired; }

  /// <summary> Check if user can vote on this comment </summary>
  public bool CanVote { get => HasPermission(PermissionType.Vote) && !IsExpired; }
}
