using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using GameGuild.Identity.Users;

namespace GameGuild.Projects;

/// <summary> Stub for Team entity - TODO: Implement full Teams module </summary>
[Table("Teams")]
public class Team : EntityBase {
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}

/// <summary> Stub for TeamMember entity - TODO: Implement full Teams module </summary>
[Table("TeamMembers")]
public class TeamMember : EntityBase {
    public Guid TeamId { get; set; }
    
    public virtual Team? Team { get; set; }
    
    public Guid UserId { get; set; }
    
    public virtual User? User { get; set; }
    
    [MaxLength(100)]
    public string Role { get; set; } = "Member";
    
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
