using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Permission class for TestingLocation entities
/// Manages access control for testing locations
/// </summary>
public class TestingLocationPermission : ResourcePermission<TestingLocation> {
  /// <summary>
  /// Initialize TestingLocation permission
  /// </summary>
  /// <param name="permissionType">The type of permission required</param>
  public TestingLocationPermission(PermissionType permissionType) : base(permissionType) { }
}
