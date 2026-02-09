using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Projects;

/// <summary> Stub for Jam entity - PLANNED: Replace with full Jam module </summary>
[Table("Jams")]
public class Jam : EntityBase {
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public bool IsActive { get; set; }
}

/// <summary> Stub for JamScore entity - PLANNED: Replace with full Jam module </summary>
[Table("JamScores")]
public class JamScore : EntityBase {
    public Guid JamSubmissionId { get; set; }
    
    public ProjectJamSubmission? JamSubmission { get; set; }
    
    public Guid JudgeId { get; set; }
    
    public decimal Score { get; set; }
    
    [MaxLength(500)]
    public string? Category { get; set; }
    
    [MaxLength(2000)]
    public string? Comments { get; set; }
}
