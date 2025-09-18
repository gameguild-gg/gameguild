using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Projects;

/// <summary> Resource-specific permissions for Project entities (Layer 3 of the DAC permission system) Provide granular permission control for individual projects </summary>
[Table("ProjectPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true, Name = "IX_ProjectPermissions_User_Tenant_Resource")]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProjectPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProjectPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProjectPermissions_Expiration")]
public class ProjectPermission : ResourcePermission<Project> {
  // Public parameterless constructor for EF and GraphQL
  public ProjectPermission() : base() { }

  // Public constructor for creating instances
  public ProjectPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) { }

  // Project-specific computed properties

  /// <summary> Check if the user can edit this specific project </summary>
  public bool CanEdit { get => HasPermission(PermissionType.Edit) && !IsExpired; }

  /// <summary> Check if the user can delete this specific project </summary>
  public bool CanDelete { get => HasPermission(PermissionType.Delete) && !IsExpired; }

  /// <summary> Check if a user can publish this specific project </summary>
  public bool CanPublish { get => HasPermission(PermissionType.Publish) && !IsExpired; }

  /// <summary> Check if the user can manage collaborators for this specific project </summary>
  public bool CanManageCollaborators { get => HasPermission(PermissionType.Share) && !IsExpired; }

  /// <summary> Check if a user can create releases for this specific project </summary>
  public bool CanCreateReleases { get => HasPermission(PermissionType.Create) && !IsExpired; }

  /// <summary> Check if the user can view analytics for this specific project </summary>
  public bool CanViewAnalytics { get => HasPermission(PermissionType.Analytics) && !IsExpired; }

  /// <summary> Check if the user can moderate content for this specific project </summary>
  public bool CanModerate { get => HasPermission(PermissionType.Review) && !IsExpired; }

  /// <summary> Check if the user can archive this specific project </summary>
  public bool CanArchive { get => HasPermission(PermissionType.Archive) && !IsExpired; }

  /// <summary> Check if the user can transfer ownership of this specific project </summary>
  public bool CanTransferOwnership { get => HasPermission(PermissionType.License) && !IsExpired; }

  /// <summary> Check if a user can download releases from this specific project </summary>
  public bool CanDownload { get => HasPermission(PermissionType.Read) && !IsExpired; }

  /// <summary> Check if the user can fork this specific project </summary>
  public bool CanFork { get => HasPermission(PermissionType.Clone) && !IsExpired; }
}
