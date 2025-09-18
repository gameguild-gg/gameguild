namespace GameGuild.Modules.TestingLab;

/// <summary> Permission class for SessionWaitlist entities Manages access control for session waitlists </summary>
public class SessionWaitlistPermission : ResourcePermission<SessionWaitlist> {
  public SessionWaitlistPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) { }
}
