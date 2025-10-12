namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Represents an invitation to join a team.
/// </summary>
public class TeamInvitation : EntityBase
{
    /// <summary>
    /// Gets or sets the team ID.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the invitee.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID of the invitee (if registered).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who sent the invitation.
    /// </summary>
    public string InvitedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role the invitee will have upon acceptance.
    /// </summary>
    public TeamRole Role { get; set; } = TeamRole.Member;

    /// <summary>
    /// Gets or sets the invitation token for verification.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the invitation was accepted.
    /// </summary>
    public bool IsAccepted { get; set; }

    /// <summary>
    /// Gets or sets when the invitation was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the invitation was declined.
    /// </summary>
    public bool IsDeclined { get; set; }

    /// <summary>
    /// Gets or sets when the invitation was declined.
    /// </summary>
    public DateTime? DeclinedAt { get; set; }

    /// <summary>
    /// Gets or sets a personal message from the inviter.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Navigation property to the team.
    /// </summary>
    public Team? Team { get; set; }
}
