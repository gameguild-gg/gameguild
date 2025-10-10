namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Represents a member of a team with role and status information.
/// </summary>
public class TeamMember : EntityBase
{
    /// <summary>
    /// Gets or sets the team ID.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the user ID (external user reference).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member's role in the team.
    /// </summary>
    public TeamRole Role { get; set; } = TeamRole.Member;

    /// <summary>
    /// Gets or sets the member's status.
    /// </summary>
    public MemberStatus Status { get; set; } = MemberStatus.Invited;

    /// <summary>
    /// Gets or sets the user ID who invited this member.
    /// </summary>
    public string? InvitedBy { get; set; }

    /// <summary>
    /// Gets or sets when the member joined the team.
    /// </summary>
    public DateTime? JoinedAt { get; set; }

    /// <summary>
    /// Gets or sets when the member left the team.
    /// </summary>
    public DateTime? LeftAt { get; set; }

    /// <summary>
    /// Gets or sets when the member was suspended.
    /// </summary>
    public DateTime? SuspendedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for suspension.
    /// </summary>
    public string? SuspensionReason { get; set; }

    /// <summary>
    /// Gets or sets additional notes about the member.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property to the team.
    /// </summary>
    public Team? Team { get; set; }
}
