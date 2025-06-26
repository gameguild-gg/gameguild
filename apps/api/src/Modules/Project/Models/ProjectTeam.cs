using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Models;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a team working on a project
/// </summary>
[Table("ProjectTeams")]
[Index(nameof(ProjectId), nameof(TeamId), IsUnique = true, Name = "IX_ProjectTeams_Project_Team")]
[Index(nameof(TeamId), Name = "IX_ProjectTeams_Team")]
[Index(nameof(AssignedAt), Name = "IX_ProjectTeams_Date")]
public class ProjectTeam : ResourceBase
{
    private Guid _projectId;

    private Project _project = null!;

    private Guid _teamId;

    private Team _team = null!;

    private string _role = "Development";

    private DateTime _assignedAt = DateTime.UtcNow;

    private DateTime? _endedAt;

    private bool _isActive = true;

    private string? _permissions;

    private string? _notes;

    private decimal _contributionPercentage = 0;

    /// <summary>
    /// Project the team is working on
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
    /// Team working on the project
    /// </summary>
    public Guid TeamId
    {
        get => _teamId;
        set => _teamId = value;
    }

    /// <summary>
    /// Navigation property to team
    /// </summary>
    public virtual GameGuild.Models.Team Team
    {
        get => _team;
        set => _team = value;
    }

    /// <summary>
    /// Role of the team in the project
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Role
    {
        get => _role;
        set => _role = value;
    }

    /// <summary>
    /// Date when the team was assigned to the project
    /// </summary>
    public DateTime AssignedAt
    {
        get => _assignedAt;
        set => _assignedAt = value;
    }

    /// <summary>
    /// Date when the team's involvement ended (if applicable)
    /// </summary>
    public DateTime? EndedAt
    {
        get => _endedAt;
        set => _endedAt = value;
    }

    /// <summary>
    /// Whether the team is currently active on this project
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    /// <summary>
    /// Permissions granted to this team for the project
    /// </summary>
    [MaxLength(1000)]
    public string? Permissions
    {
        get => _permissions;
        set => _permissions = value;
    }

    /// <summary>
    /// Notes about the team's involvement
    /// </summary>
    [MaxLength(1000)]
    public string? Notes
    {
        get => _notes;
        set => _notes = value;
    }

    /// <summary>
    /// Team's contribution percentage (0-100)
    /// </summary>
    [Range(0, 100)]
    public decimal ContributionPercentage
    {
        get => _contributionPercentage;
        set => _contributionPercentage = value;
    }
}
