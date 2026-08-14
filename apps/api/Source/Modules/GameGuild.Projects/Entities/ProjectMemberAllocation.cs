using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using GameGuild.Identity.Users;

namespace GameGuild.Projects;

[Table("project_member_allocations")]
public sealed class ProjectMemberAllocation : EntityBase
{
    public Guid ProjectId { get; set; }
    [JsonIgnore]
    public Project Project { get; set; } = null!;
    public Guid ProjectTeamId { get; set; }
    [JsonIgnore]
    public ProjectTeam ProjectTeam { get; set; } = null!;
    public Guid UserId { get; set; }
    [JsonIgnore]
    public User? User { get; set; }

    [Required, MaxLength(100)]
    public string Function { get; set; } = string.Empty;

    [Range(1, 100)]
    public decimal CapacityPercentage { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}
