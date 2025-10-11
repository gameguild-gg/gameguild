using GameGuild.Modules.Resources;
using GameGuild.Modules.TestingLab.Entities;

namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Permission class for TestingLocation entities
/// Manages access control for testing locations
/// </summary>
public class TestingLocationPermission : GameGuild.Modules.Resources.ResourcePermission<TestingLocation> {
  public TestingLocationPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId) {
    AddPermission(permissions);
  }
}
