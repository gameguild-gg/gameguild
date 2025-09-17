using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// Permission class for SessionWaitlist entities
/// Manages access control for session waitlists
/// </summary>
public class SessionWaitlistPermission : ResourcePermission<SessionWaitlist> {
    /// <summary>
    /// Initialize SessionWaitlist permission
    /// </summary>
    /// <param name="permissionType">The type of permission required</param>
    public SessionWaitlistPermission(PermissionType permissionType) : base(permissionType) { }
}
