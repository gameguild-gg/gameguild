using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a collaborator on a project
/// </summary>
[Table("ProjectCollaborators")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectCollaborators_Project_User")]
[Index(nameof(UserId), Name = "IX_ProjectCollaborators_User")]
public class ProjectCollaborator : ResourceBase
{
    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId
    {
        get;
        set;
    }

    /// <summary>
    /// Navigation property to project
    /// </summary>
    public virtual Project Project
    {
        get;
        set;
    } = null!;

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId
    {
        get;
        set;
    }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User.Models.User User
    {
        get;
        set;
    } = null!;

    /// <summary>
    /// Role of the collaborator in the project
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// Permissions granted to this collaborator
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Permissions
    {
        get;
        set;
    } = string.Empty;

    /// <summary>
    /// Whether the collaborator is active
    /// </summary>
    public bool IsActive
    {
        get;
        set;
    } = true;

    /// <summary>
    /// Date when the collaboration started
    /// </summary>
    public DateTime JoinedAt
    {
        get;
        set;
    } = DateTime.UtcNow;

    /// <summary>
    /// Date when the collaboration ended (if applicable)
    /// </summary>
    public DateTime? LeftAt
    {
        get;
        set;
    }
}
