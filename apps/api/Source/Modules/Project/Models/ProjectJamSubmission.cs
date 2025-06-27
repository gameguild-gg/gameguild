using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;
using GameGuild.Modules.Jam.Models;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a project submitted to a game jam
/// </summary>
[Table("ProjectJamSubmissions")]
[Index(nameof(ProjectId), nameof(JamId), IsUnique = true, Name = "IX_ProjectJamSubmissions_Project_Jam")]
[Index(nameof(JamId), Name = "IX_ProjectJamSubmissions_Jam")]
[Index(nameof(SubmittedAt), Name = "IX_ProjectJamSubmissions_Date")]
[Index(nameof(FinalScore), Name = "IX_ProjectJamSubmissions_Score")]
public class ProjectJamSubmission : ResourceBase {
  private Guid _projectId;

  private Project _project = null!;

  private Guid _jamId;

  private Jam.Models.Jam _jam = null!;

  private DateTime _submittedAt = DateTime.UtcNow;

  private bool _isEligible = true;

  private string? _submissionNotes;

  private decimal? _finalScore;

  private int? _ranking;

  private bool _hasAward = false;

  private string? _awardDetails;

  private string? _metadata;

  private ICollection<JamScore> _scores = new List<Jam.Models.JamScore>();

  /// <summary>
  /// Project being submitted
  /// </summary>
  public Guid ProjectId {
    get => _projectId;
    set => _projectId = value;
  }

  /// <summary>
  /// Navigation property to project
  /// </summary>
  public virtual Project Project {
    get => _project;
    set => _project = value;
  }

  /// <summary>
  /// Jam the project is submitted to
  /// </summary>
  public Guid JamId {
    get => _jamId;
    set => _jamId = value;
  }

  /// <summary>
  /// Navigation property to jam
  /// </summary>
  public virtual Jam.Models.Jam Jam {
    get => _jam;
    set => _jam = value;
  }

  /// <summary>
  /// Date when the project was submitted to the jam
  /// </summary>
  public DateTime SubmittedAt {
    get => _submittedAt;
    set => _submittedAt = value;
  }

  /// <summary>
  /// Whether the submission is eligible for judging
  /// </summary>
  public bool IsEligible {
    get => _isEligible;
    set => _isEligible = value;
  }

  /// <summary>
  /// Submission notes or description
  /// </summary>
  [MaxLength(2000)]
  public string? SubmissionNotes {
    get => _submissionNotes;
    set => _submissionNotes = value;
  }

  /// <summary>
  /// Final score calculated from all ratings
  /// </summary>
  public decimal? FinalScore {
    get => _finalScore;
    set => _finalScore = value;
  }

  /// <summary>
  /// Ranking in the jam (if calculated)
  /// </summary>
  public int? Ranking {
    get => _ranking;
    set => _ranking = value;
  }

  /// <summary>
  /// Whether this submission won an award
  /// </summary>
  public bool HasAward {
    get => _hasAward;
    set => _hasAward = value;
  }

  /// <summary>
  /// Award details (JSON)
  /// </summary>
  [MaxLength(1000)]
  public string? AwardDetails {
    get => _awardDetails;
    set => _awardDetails = value;
  }

  /// <summary>
  /// Additional submission metadata
  /// </summary>
  [MaxLength(2000)]
  public new string? Metadata {
    get => _metadata;
    set => _metadata = value;
  }

  /// <summary>
  /// Navigation property to jam scores
  /// </summary>
  public virtual ICollection<Jam.Models.JamScore> Scores {
    get => _scores;
    set => _scores = value;
  }
}
