using GameGuild.Modules.Certificates;
using GameGuild.Modules.Feedbacks;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Junction entity representing learner enrollment and progress in a program
/// </summary>
/// <remarks>
/// ProgramUser tracks the complete learner journey within a program including:
/// - Enrollment and activation status
/// - Progress tracking and completion percentage
/// - Grade tracking and certification pathways
/// - Access patterns and engagement analytics
/// 
/// This entity serves as the central hub for learner-program relationships,
/// enabling progress tracking, grade management, and completion certification.
/// Inherits from EntityBase for UUID IDs, versioning, timestamps, and soft delete.
/// </remarks>
[Table("program_users")]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(JoinedAt))]
[Index(nameof(IsActive))]
[Index(nameof(CompletionPercentage))]
public class ProgramUser : EntityBase {
  /// <summary>
  /// Default constructor for entity framework and general instantiation
  /// </summary>
  public ProgramUser() { }

  /// <summary>
  /// Constructor for partial initialization with dynamic object properties
  /// </summary>
  /// <param name="partial">Object containing partial program user data for initialization</param>
  /// <remarks>
  /// Enables flexible initialization from DTOs or dynamic objects while preserving
  /// base entity functionality for timestamps and soft delete.
  /// </remarks>
  public ProgramUser(object partial) : base(partial) { }

  /// <summary>
  /// Foreign key reference to the enrolled user
  /// </summary>
  /// <remarks>
  /// Establishes the user side of the enrollment relationship.
  /// Combined with ProgramId creates a unique enrollment record.
  /// </remarks>
  [Required]
  public Guid UserId { get; set; }

  /// <summary>
  /// Navigation property to the enrolled user entity
  /// </summary>
  /// <remarks>
  /// Provides access to user profile, preferences, and authentication details
  /// for personalized learning experiences and communication.
  /// </remarks>
  [ForeignKey(nameof(UserId))]
  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Foreign key reference to the program being accessed
  /// </summary>
  /// <remarks>
  /// Establishes the program side of the enrollment relationship.
  /// Combined with UserId creates a unique enrollment record.
  /// </remarks>
  [Required]
  public Guid ProgramId { get; set; }

  /// <summary>
  /// Navigation property to the program entity
  /// </summary>
  /// <remarks>
  /// Provides access to program content, structure, and requirements
  /// for progress tracking and completion validation.
  /// </remarks>
  [ForeignKey(nameof(ProgramId))]
  public virtual Program Program { get; set; } = null!;

  /// <summary>
  /// Indicates whether this enrollment is currently active and accessible
  /// </summary>
  /// <remarks>
  /// Controls learner access to program content and features:
  /// - Active: Full access to all enrolled program content
  /// - Inactive: Suspended access, progress preserved
  /// Used for enrollment management and access control.
  /// </remarks>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Timestamp when the user first enrolled in this program
  /// </summary>
  /// <remarks>
  /// Immutable enrollment date used for:
  /// - Cohort grouping and analytics
  /// - Enrollment period tracking
  /// - Historical enrollment reporting
  /// </remarks>
  public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Overall completion percentage for the program (0-100)
  /// </summary>
  /// <remarks>
  /// Calculated based on completed required content items.
  /// Used for:
  /// - Progress visualization and motivation
  /// - Completion certification eligibility
  /// - Analytics and reporting
  /// Updates automatically as content is completed.
  /// </remarks>
  [Column(TypeName = "decimal(5,2)")]
  public decimal CompletionPercentage { get; set; }

  /// <summary>
  /// Overall weighted grade for the program (0-100)
  /// </summary>
  /// <remarks>
  /// Calculated from all graded content items within the program.
  /// Null indicates no gradeable content has been completed yet.
  /// Used for certification, transcripts, and academic records.
  /// </remarks>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? FinalGrade { get; set; }

  /// <summary>
  /// Timestamp when the user first accessed any program content
  /// </summary>
  /// <remarks>
  /// Marks the beginning of active learning engagement.
  /// Differs from JoinedAt which tracks enrollment.
  /// Used for engagement analytics and learning pattern analysis.
  /// </remarks>
  public DateTime? StartedAt { get; set; }

  /// <summary>
  /// Timestamp when the user completed all required program content
  /// </summary>
  /// <remarks>
  /// Automatically set when CompletionPercentage reaches 100%.
  /// Triggers certification eligibility and completion notifications.
  /// Used for graduation tracking and completion certificates.
  /// </remarks>
  public DateTime? CompletedAt { get; set; }

  /// <summary>
  /// Timestamp of the user's most recent content access within this program
  /// </summary>
  /// <remarks>
  /// Updated on any content interaction (view, submission, completion).
  /// Used for:
  /// - Engagement tracking and analytics
  /// - Re-engagement campaign triggers
  /// - Learning pattern analysis
  /// </remarks>
  public DateTime? LastAccessedAt { get; set; }

  // Navigation properties for related entities

  /// <summary>
  /// Collection of all content interactions within this program enrollment
  /// </summary>
  /// <remarks>
  /// Tracks detailed engagement including views, time spent, submissions.
  /// Used for analytics, progress calculation, and learning behavior analysis.
  /// </remarks>
  public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = [];

  /// <summary>
  /// Collection of grades received by this user in program activities
  /// </summary>
  /// <remarks>
  /// Contains all assessment results for gradeable content within the program.
  /// Used for transcript generation and final grade calculation.
  /// </remarks>
  public virtual ICollection<ActivityGrade> ReceivedGrades { get; set; } = [];

  /// <summary>
  /// Collection of grades given by this user in peer review activities
  /// </summary>
  /// <remarks>
  /// Tracks peer assessment contributions when user acts as a reviewer.
  /// Used for peer review quality tracking and reciprocal grading systems.
  /// </remarks>
  public virtual ICollection<ActivityGrade> GivenGrades { get; set; } = [];

  /// <summary>
  /// Collection of certificates earned through this program enrollment
  /// </summary>
  /// <remarks>
  /// Contains all certifications achieved within this program context.
  /// Linked to program completion and competency demonstration.
  /// </remarks>
  public virtual ICollection<UserCertificate> UserCertificates { get; set; } = [];

  /// <summary>
  /// Collection of feedback submissions for program evaluation
  /// </summary>
  /// <remarks>
  /// Contains learner feedback on program quality, content, and experience.
  /// Used for continuous program improvement and quality assurance.
  /// </remarks>
  public virtual ICollection<ProgramFeedbackSubmission> FeedbackSubmissions { get; set; } = [];

  /// <summary>
  /// Collection of ratings given by this user for program evaluation
  /// </summary>
  /// <remarks>
  /// Contains numerical ratings for overall program satisfaction.
  /// Contributes to program quality metrics and discovery algorithms.
  /// </remarks>
  public virtual ICollection<ProgramRating> ProgramRatings { get; set; } = [];

  /// <summary>
  /// Activates the enrollment to grant access to program content
  /// </summary>
  /// <remarks>
  /// Enables learner access to all program features and content.
  /// Updates the entity timestamp to track status change.
  /// Used for enrollment restoration and access management.
  /// </remarks>
  public void Activate() {
    IsActive = true;
    Touch();
  }

  /// <summary>
  /// Deactivates the enrollment to suspend access while preserving progress
  /// </summary>
  /// <remarks>
  /// Suspends learner access without losing progress or enrollment data.
  /// Updates the entity timestamp to track status change.
  /// Used for temporary suspensions and access control.
  /// </remarks>
  public void Deactivate() {
    IsActive = false;
    Touch();
  }

  /// <summary>
  /// Records content access activity and updates engagement timestamp
  /// </summary>
  /// <remarks>
  /// Called whenever the user interacts with any program content.
  /// Updates LastAccessedAt for engagement tracking and analytics.
  /// Triggers entity versioning for audit purposes.
  /// </remarks>
  public void MarkAccessed() {
    LastAccessedAt = DateTime.UtcNow;
    Touch();
  }

  /// <summary>
  /// Initiates active learning by recording the first content access
  /// </summary>
  /// <remarks>
  /// Sets StartedAt timestamp only on first program engagement.
  /// Distinguishes between enrollment (JoinedAt) and active learning (StartedAt).
  /// Used for measuring time-to-engagement and learning analytics.
  /// </remarks>
  public void StartProgram() {
    if (StartedAt == null) {
      StartedAt = DateTime.UtcNow;
      Touch();
    }
  }

  /// <summary>
  /// Marks the program as fully completed with timestamp and percentage
  /// </summary>
  /// <remarks>
  /// Sets completion timestamp and ensures 100% completion percentage.
  /// Triggers certification eligibility and graduation processes.
  /// Updates entity versioning for completion tracking.
  /// </remarks>
  public void CompleteProgram() {
    CompletedAt = DateTime.UtcNow;
    CompletionPercentage = 100;
    Touch();
  }
}
