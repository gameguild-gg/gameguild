using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using GameGuild.Identity.Users;

namespace GameGuild.Projects;

/// <summary>
/// Represents a collaborator on a project
/// </summary>
[Table("ProjectCollaborators")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectCollaborators_Project_User")]
[Index(nameof(UserId), Name = "IX_ProjectCollaborators_User")]
public class ProjectCollaborator : EntityBase<Guid>
{
    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to project
    /// </summary>
    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual User? User { get; set; }

    /// <summary>
    /// Role of the collaborator in the project
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Permissions granted to this collaborator
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Permissions { get; set; } = string.Empty;

    /// <summary>
    /// Whether the collaborator is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when the collaboration started
    /// </summary>
    public DateTime JoinedAt { get; set; } = SystemClock.UtcNow;

    /// <summary>
    /// Date when the collaboration ended (if applicable)
    /// </summary>
    public DateTime? LeftAt { get; set; }
}
