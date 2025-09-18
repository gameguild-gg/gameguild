namespace GameGuild.Modules.TestingLab;

/// <summary> Permission class for TestingParticipant entities Manages access control for testing participants </summary>
public class TestingParticipantPermission : ResourcePermission<TestingParticipant> {
  public TestingParticipantPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) { }
}
