namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Defines the role of a team member within a team.
/// </summary>
public enum TeamRole
{
    /// <summary>
    /// Team owner with full administrative privileges.
    /// Can manage all aspects of the team including deletion.
    /// </summary>
    Owner = 1,

    /// <summary>
    /// Administrator with most privileges except transferring ownership.
    /// Can manage members, settings, and content.
    /// </summary>
    Admin = 2,

    /// <summary>
    /// Regular member with standard access.
    /// Can contribute and collaborate on team resources.
    /// </summary>
    Member = 3,

    /// <summary>
    /// Viewer with read-only access.
    /// Can view team content but cannot make changes.
    /// </summary>
    Viewer = 4
}
