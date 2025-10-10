namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Defines the status of a team member.
/// </summary>
public enum MemberStatus
{
    /// <summary>
    /// Member is actively participating in the team.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Member has been invited but has not yet accepted.
    /// </summary>
    Invited = 2,

    /// <summary>
    /// Member account is temporarily suspended.
    /// Access is restricted but membership is preserved.
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// Member has left the team or been removed.
    /// </summary>
    Left = 4
}
