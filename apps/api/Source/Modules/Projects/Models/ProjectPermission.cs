using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects.Models;

/// <summary>
/// Resource-specific permissions for Project entities (Layer 3 of the DAC permission system)
/// Provide granular permission control for individual projects
/// </summary>
[Table("ProjectPermissions")]
[Index(
  nameof(UserId),
  nameof(TenantId),
  nameof(ResourceId),
  IsUnique = true,
  Name = "IX_ProjectPermissions_User_Tenant_Resource"
)]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProjectPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProjectPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProjectPermissions_Expiration")]
public class ProjectPermission : ResourcePermission<Project> {
  // Project-specific computed properties

  /// <summary>
  /// Check if the user can edit this specific project
  /// </summary>
  public bool CanEdit {
    get => HasPermission(PermissionType.Edit) && IsValid;
  }

  /// <summary>
  /// Check if the user can delete this specific project
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete) && IsValid;
  }

  /// <summary>
  /// Check if a user can publish this specific project
  /// </summary>
  public bool CanPublish {
    get => HasPermission(PermissionType.Publish) && IsValid;
  }

  /// <summary>
  /// Check if the user can manage collaborators for this specific project
  /// </summary>
  public bool CanManageCollaborators {
    get => HasPermission(PermissionType.Share) && IsValid;
  }

  /// <summary>
  /// Check if a user can create releases for this specific project
  /// </summary>
  public bool CanCreateReleases {
    get => HasPermission(PermissionType.Create) && IsValid;
  }

  /// <summary>
  /// Check if the user can view analytics for this specific project
  /// </summary>
  public bool CanViewAnalytics {
    get => HasPermission(PermissionType.Analytics) && IsValid;
  }

  /// <summary>
  /// Check if user can moderate this project (e.g., handle reports, moderate comments)
  /// </summary>
  public bool CanModerate {
    get => HasPermission(PermissionType.Review) && IsValid;
  }

  /// <summary>
  /// Check if user can archive/restore this specific project
  /// </summary>
  public bool CanArchive {
    get => HasPermission(PermissionType.Archive) && IsValid;
  }

  /// <summary>
  /// Check if user can transfer ownership of this specific project
  /// </summary>
  public bool CanTransferOwnership {
    get => HasPermission(PermissionType.License) && IsValid;
  }

  /// <summary>
  /// Check if a user can download releases from this specific project
  /// </summary>
  public bool CanDownload {
    get => HasPermission(PermissionType.Read) && IsValid;
  }

  /// <summary>
  /// Check if the user can fork this specific project
  /// </summary>
  public bool CanFork {
    get => HasPermission(PermissionType.Clone) && IsValid;
  }
}
