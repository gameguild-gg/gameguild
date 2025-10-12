namespace GameGuild.Modules.Teams.Entities;

/// <summary>
/// Represents permissions for team access control.
/// </summary>
public class TeamPermission : EntityBase
{
    /// <summary>
    /// Gets or sets the team ID.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Gets or sets the resource type (e.g., "Project", "Document").
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific resource ID.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the permission name (e.g., "read", "write", "delete").
    /// </summary>
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum role required for this permission.
    /// </summary>
    public TeamRole MinimumRole { get; set; } = TeamRole.Member;

    /// <summary>
    /// Gets or sets whether the permission is granted.
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Gets or sets the user ID who granted the permission.
    /// </summary>
    public string? GrantedBy { get; set; }

    /// <summary>
    /// Gets or sets when the permission was granted.
    /// </summary>
    public DateTime? GrantedAt { get; set; }

    /// <summary>
    /// Gets or sets when the permission expires (if applicable).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Navigation property to the team.
    /// </summary>
    public Team? Team { get; set; }
}
