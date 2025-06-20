using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a project submitted to a game jam
/// </summary>
[Table("ProjectJamSubmissions")]
[Index(nameof(ProjectId), nameof(JamId), IsUnique = true, Name = "IX_ProjectJamSubmissions_Project_Jam")]
[Index(nameof(JamId), Name = "IX_ProjectJamSubmissions_Jam")]
[Index(nameof(SubmittedAt), Name = "IX_ProjectJamSubmissions_Date")]
[Index(nameof(FinalScore), Name = "IX_ProjectJamSubmissions_Score")]
public class ProjectJamSubmission : ResourceBase
{
    /// <summary>
    /// Project being submitted
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to project
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// Jam the project is submitted to
    /// </summary>
    public Guid JamId { get; set; }

    /// <summary>
    /// Navigation property to jam
    /// </summary>
    public virtual Jam.Models.Jam Jam { get; set; } = null!;

    /// <summary>
    /// Date when the project was submitted to the jam
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the submission is eligible for judging
    /// </summary>
    public bool IsEligible { get; set; } = true;

    /// <summary>
    /// Submission notes or description
    /// </summary>
    [MaxLength(2000)]
    public string? SubmissionNotes { get; set; }

    /// <summary>
    /// Final score calculated from all ratings
    /// </summary>
    public decimal? FinalScore { get; set; }

    /// <summary>
    /// Ranking in the jam (if calculated)
    /// </summary>
    public int? Ranking { get; set; }

    /// <summary>
    /// Whether this submission won an award
    /// </summary>
    public bool HasAward { get; set; } = false;

    /// <summary>
    /// Award details (JSON)
    /// </summary>
    [MaxLength(1000)]
    public string? AwardDetails { get; set; }

    /// <summary>
    /// Additional submission metadata
    /// </summary>
    [MaxLength(2000)]
    public new string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to jam scores
    /// </summary>
    public virtual ICollection<Jam.Models.JamScore> Scores { get; set; } = new List<Jam.Models.JamScore>();
}
