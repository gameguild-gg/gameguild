using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Projects;

/// <summary> Represents a team working on a project </summary>
[Table("ProjectTeams")]
[Index(nameof(ProjectId), nameof(TeamId), IsUnique = true, Name = "IX_ProjectTeams_Project_Team")]
[Index(nameof(TeamId), Name = "IX_ProjectTeams_Team")]
[Index(nameof(AssignedAt), Name = "IX_ProjectTeams_Date")]
public class ProjectTeam : EntityBase<Guid>
{
    /// <summary> Project the team is working on </summary>
    public Guid ProjectId { get; set; }

    /// <summary> Navigation property to project </summary>
    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    /// <summary> Team working on the project </summary>
    public Guid TeamId { get; set; }

    /// <summary> Navigation property to the project collaboration team. </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public virtual Team? Team { get; set; }

    /// <summary> Role of the team in the project </summary>
    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = "Development";

    /// <summary> Date when the team was assigned to the project </summary>
    public DateTime AssignedAt { get; set; } = SystemClock.UtcNow;

    /// <summary> Date when the team's involvement ended (if applicable) </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary> Whether the team is currently active on this project </summary>
    public bool IsActive { get; set; } = true;

    /// <summary> Permissions granted to this team for the project </summary>
    [MaxLength(1000)]
    public string? Permissions { get; set; }

    /// <summary> Notes about the team's involvement </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary> Team's contribution percentage (0-100) </summary>
    [Range(0, 100)]
    public decimal ContributionPercentage { get; set; } = 0;
}
