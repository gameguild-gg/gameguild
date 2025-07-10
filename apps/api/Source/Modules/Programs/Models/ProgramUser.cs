using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Certificates.Models;
using GameGuild.Modules.Feedbacks.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Junction entity representing the relationship between a User and a Program
/// Inherits from BaseEntity to provide UUID IDs, version control, timestamps, and soft delete functionality
/// </summary>
[Table("program_users")]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(JoinedAt))]
[Index(nameof(IsActive))]
[Index(nameof(CompletionPercentage))]
public class ProgramUser : Entity {
  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Foreign key to the Program entity
  /// </summary>
  [Required]
  public Guid ProgramId { get; set; }

  /// <summary>
  /// Navigation property to the Program entity
  /// </summary>
  [ForeignKey(nameof(ProgramId))]
  public virtual Program Program { get; set; } = null!;

  /// <summary>
  /// Whether this user-program relationship is currently active
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// When the user joined this program
  /// </summary>
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Overall completion percentage for the program (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal CompletionPercentage { get; set; }

  /// <summary>
  /// Overall grade for the program (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? FinalGrade { get; set; }

  /// <summary>
  /// Date when user started this program
  /// </summary>
  public DateTime? StartedAt { get; set; }

  /// <summary>
  /// Date when user completed this program
  /// </summary>
  public DateTime? CompletedAt { get; set; }

  /// <summary>
  /// Last date user accessed any content in this program
  /// </summary>
  public DateTime? LastAccessedAt { get; set; }

  public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = new List<ContentInteraction>();

  public virtual ICollection<ActivityGrade> ReceivedGrades { get; set; } = new List<ActivityGrade>();

  public virtual ICollection<ActivityGrade> GivenGrades { get; set; } = new List<ActivityGrade>();

  public virtual ICollection<UserCertificate> UserCertificates { get; set; } = new List<UserCertificate>();

  public virtual ICollection<ProgramFeedbackSubmission> FeedbackSubmissions { get; set; } = new List<ProgramFeedbackSubmission>();

  public virtual ICollection<ProgramRating> ProgramRatings { get; set; } = new List<ProgramRating>();

  /// <summary>
  /// Default constructor
  /// </summary>
  public ProgramUser() { }

  /// <summary>
  /// Constructor for partial initialization
  /// </summary>
  /// <param name="partial">Partial program user data</param>
  public ProgramUser(object partial) : base(partial) { }

  /// <summary>
  /// Activate the user-program relationship
  /// </summary>
  public void Activate() {
    IsActive = true;
    Touch();
  }

  /// <summary>
  /// Deactivate the user-program relationship
  /// </summary>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }

  /// <summary>
  /// Mark content as accessed and update the last accessed timestamp
  /// </summary>
  public void MarkAccessed() {
    LastAccessedAt = DateTime.UtcNow;
    Touch();
  }

  /// <summary>
  /// Start the program by setting the started timestamp
  /// </summary>
  public void StartProgram() {
    if (StartedAt == null) {
      StartedAt = DateTime.UtcNow;
      Touch();
    }
  }

  /// <summary>
  /// Complete the program by setting completion timestamp and percentage
  /// </summary>
  public void CompleteProgram() {
    CompletedAt = DateTime.UtcNow;
    CompletionPercentage = 100;
    Touch();
  }
}
