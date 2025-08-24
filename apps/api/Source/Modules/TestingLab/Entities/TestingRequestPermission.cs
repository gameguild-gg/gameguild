using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;

namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Permission entity for testing request resource-level access control
/// This entity manages permissions for specific testing request resources within the 3-layer DAC system
/// </summary>
public class TestingRequestPermission : ResourcePermission<TestingRequest> {
  /// <summary>
  /// Check if user can view this testing request
  /// </summary>
  public bool CanView {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can edit this testing request
  /// </summary>
  public bool CanEdit {
    get => HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can delete this testing request
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete);
  }

  /// <summary>
  /// Check if user can manage this testing request (includes scheduling, approval, etc.)
  /// </summary>
  public bool CanManage {
    get => HasPermission(PermissionType.Approve) || HasPermission(PermissionType.Edit);
  }

  /// <summary>
  /// Check if user can participate in this testing request
  /// </summary>
  public bool CanParticipate {
    get => HasPermission(PermissionType.Read);
  }

  /// <summary>
  /// Check if user can provide feedback on this testing request
  /// </summary>
  public bool CanProvideFeedback {
    get => HasPermission(PermissionType.Comment);
  }

  /// <summary>
  /// Check if user can review this testing request
  /// </summary>
  public bool CanReview {
    get => HasPermission(PermissionType.Review);
  }

  /// <summary>
  /// Check if user can approve or reject this testing request
  /// </summary>
  public bool CanApprove {
    get => HasPermission(PermissionType.Approve);
  }
}
