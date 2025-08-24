using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Resource-specific permissions for TestingFeedback entities (Layer 3 of DAC permission system)
/// Manages access control for individual testing feedback entries
/// </summary>
[Table("TestingFeedbackPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), Name = "IX_TestingFeedbackPermissions_User_Tenant_Resource")]
public class TestingFeedbackPermission : ResourcePermission<TestingFeedback> {
  /// <summary>
  /// Check if user can view this testing feedback
  /// </summary>
  public bool CanView {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can edit this testing feedback
  /// </summary>
  public bool CanEdit {
    get => HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can delete this testing feedback
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete);
  }

  /// <summary>
  /// Check if user can manage this testing feedback (includes moderation actions)
  /// </summary>
  public bool CanManage {
    get => HasPermission(PermissionType.Approve) || HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can report this testing feedback
  /// </summary>
  public bool CanReport {
    get => HasPermission(PermissionType.Report);
  }

  /// <summary>
  /// Check if user can rate the quality of this testing feedback
  /// </summary>
  public bool CanRateQuality {
    get => HasPermission(PermissionType.Review);
  }

  /// <summary>
  /// Check if user can create responses to this testing feedback
  /// </summary>
  public bool CanRespond {
    get => HasPermission(PermissionType.Comment);
  }

  /// <summary>
  /// Check if user can moderate this testing feedback (comprehensive permissions)
  /// </summary>
  public bool CanModerate {
    get => HasPermission(PermissionType.Review) && HasPermission(PermissionType.Approve);
  }
}
