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
    private Guid _projectId;

    private Project _project = null!;

    private Guid _userId;

    private User.Models.User _user = null!;

    private string _role = string.Empty;

    private string _permissions = string.Empty;

    private bool _isActive = true;

    private DateTime _joinedAt = DateTime.UtcNow;

    private DateTime? _leftAt;

    /// <summary>
    /// Project ID
    /// </summary>
    public Guid ProjectId
    {
        get => _projectId;
        set => _projectId = value;
    }

    /// <summary>
    /// Navigation property to project
    /// </summary>
    public virtual Project Project
    {
        get => _project;
        set => _project = value;
    }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public virtual User.Models.User User
    {
        get => _user;
        set => _user = value;
    }

    /// <summary>
    /// Role of the collaborator in the project
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role
    {
        get => _role;
        set => _role = value;
    }

    /// <summary>
    /// Permissions granted to this collaborator
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Permissions
    {
        get => _permissions;
        set => _permissions = value;
    }

    /// <summary>
    /// Whether the collaborator is active
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    /// <summary>
    /// Date when the collaboration started
    /// </summary>
    public DateTime JoinedAt
    {
        get => _joinedAt;
        set => _joinedAt = value;
    }

    /// <summary>
    /// Date when the collaboration ended (if applicable)
    /// </summary>
    public DateTime? LeftAt
    {
        get => _leftAt;
        set => _leftAt = value;
    }
}
