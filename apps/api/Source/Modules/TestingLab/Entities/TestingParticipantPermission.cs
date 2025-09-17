using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Permission class for TestingParticipant entities
/// Manages access control for testing participants
/// </summary>
public class TestingParticipantPermission : ResourcePermission<TestingParticipant> {
    /// <summary>
    /// Initialize TestingParticipant permission
    /// </summary>
    /// <param name="permissionType">The type of permission required</param>
    public TestingParticipantPermission(PermissionType permissionType) : base(permissionType) { }
}
