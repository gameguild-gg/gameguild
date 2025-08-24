using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Resource-specific permissions for TestingSession entities (Layer 3 of DAC permission system)
/// Provides granular permission control for individual testing sessions
/// </summary>
[Table("TestingSessionPermissions")]
[Index(
  nameof(UserId),
  nameof(TenantId),
  nameof(ResourceId),
  IsUnique = true,
  Name = "IX_TestingSessionPermissions_User_Tenant_Resource"
)]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_TestingSessionPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_TestingSessionPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_TestingSessionPermissions_Expiration")]
public class TestingSessionPermission : ResourcePermission<TestingSession> {
  // TestingSession-specific computed properties

  /// <summary>
  /// Check if user can manage this specific testing session
  /// </summary>
  public bool CanManage {
    get => HasPermission(PermissionType.Edit) && HasPermission(PermissionType.Delete);
  }

  /// <summary>
  /// Check if user can view this specific testing session
  /// </summary>
  public bool CanView {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can register for this testing session
  /// </summary>
  public bool CanRegister {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can provide feedback for this testing session
  /// </summary>
  public bool CanProvideFeedback {
    get => HasPermission(PermissionType.Comment);
  }

  /// <summary>
  /// Check if user can moderate this testing session
  /// </summary>
  public bool CanModerate {
    get => HasPermission(PermissionType.Review);
  }

  /// <summary>
  /// Check if user can approve or cancel this testing session
  /// </summary>
  public bool CanApprove {
    get => HasPermission(PermissionType.Approve);
  }
}
