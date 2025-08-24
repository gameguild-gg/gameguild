using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Resource-specific permissions for SessionRegistration entities (Layer 3 of DAC permission system)
/// Manages access control for individual session registration entries
/// </summary>
[Table("SessionRegistrationPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), Name = "IX_SessionRegistrationPermissions_User_Tenant_Resource")]
public class SessionRegistrationPermission : ResourcePermission<SessionRegistration> {
  /// <summary>
  /// Check if user can view this session registration
  /// </summary>
  public bool CanView {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can edit this session registration
  /// </summary>
  public bool CanEdit {
    get => HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can delete this session registration
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete);
  }

  /// <summary>
  /// Check if user can manage this session registration (includes attendance updates)
  /// </summary>
  public bool CanManage {
    get => HasPermission(PermissionType.Approve) || HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can register for sessions
  /// </summary>
  public bool CanRegister {
    get => HasPermission(PermissionType.Create);
  }

  /// <summary>
  /// Check if user can update attendance for this registration
  /// </summary>
  public bool CanUpdateAttendance {
    get => HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can approve this session registration
  /// </summary>
  public bool CanApprove {
    get => HasPermission(PermissionType.Approve);
  }

  /// <summary>
  /// Check if user can review this session registration
  /// </summary>
  public bool CanReview {
    get => HasPermission(PermissionType.Review);
  }
}
