namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Represents a team entity for collaborative work.
/// </summary>
public class Team : EntityBase
{
    /// <summary>
    /// Gets or sets the team name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the team description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the team slug for URL-friendly identification.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant ID this team belongs to.
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the team creator/owner.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the team is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the avatar/logo URL for the team.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of members allowed.
    /// </summary>
    public int? MaxMembers { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the collection of team members.
    /// </summary>
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();

    /// <summary>
    /// Gets or sets the collection of team invitations.
    /// </summary>
    public ICollection<TeamInvitation> Invitations { get; set; } = new List<TeamInvitation>();
}
