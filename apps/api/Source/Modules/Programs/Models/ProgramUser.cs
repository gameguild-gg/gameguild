using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Common.Entities;
using GameGuild.Modules.Certificate.Models;
using GameGuild.Modules.Feedback.Models;


namespace GameGuild.Modules.Program.Models;

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
public class ProgramUser : BaseEntity {
  private Guid _userId;

  private User.Models.User _user = null!;

  private Guid _programId;

  private Program _program = null!;

  private bool _isActive = true;

  private DateTime _joinedAt = DateTime.UtcNow;

  private decimal _completionPercentage = 0;

  private decimal? _finalGrade;

  private DateTime? _startedAt;

  private DateTime? _completedAt;

  private DateTime? _lastAccessedAt;

  private ICollection<ContentInteraction> _contentInteractions = new List<ContentInteraction>();

  private ICollection<ActivityGrade> _receivedGrades = new List<ActivityGrade>();

  private ICollection<ActivityGrade> _givenGrades = new List<ActivityGrade>();

  private ICollection<UserCertificate> _userCertificates = new List<Certificate.Models.UserCertificate>();

  private ICollection<ProgramFeedbackSubmission> _feedbackSubmissions =
    new List<Feedback.Models.ProgramFeedbackSubmission>();

  private ICollection<ProgramRating> _programRatings = new List<Feedback.Models.ProgramRating>();

  /// <summary>
  /// Foreign key to the User entity
  /// </summary>
  [Required]
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Foreign key to the Program entity
  /// </summary>
  [Required]
  public Guid ProgramId {
    get => _programId;
    set => _programId = value;
  }

  /// <summary>
  /// Navigation property to the Program entity
  /// </summary>
  [ForeignKey(nameof(ProgramId))]
  public virtual Program Program {
    get => _program;
    set => _program = value;
  }

  /// <summary>
  /// Whether this user-program relationship is currently active
  /// </summary>
  public bool IsActive {
    get => _isActive;
    set => _isActive = value;
  }

  /// <summary>
  /// When the user joined this program
  /// </summary>
  public DateTime JoinedAt {
    get => _joinedAt;
    set => _joinedAt = value;
  }

  /// <summary>
  /// Overall completion percentage for the program (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal CompletionPercentage {
    get => _completionPercentage;
    set => _completionPercentage = value;
  }

  /// <summary>
  /// Overall grade for the program (0-100)
  /// </summary>
  [Column(TypeName = "decimal(5,2)")]
  public decimal? FinalGrade {
    get => _finalGrade;
    set => _finalGrade = value;
  }

  /// <summary>
  /// Date when user started this program
  /// </summary>
  public DateTime? StartedAt {
    get => _startedAt;
    set => _startedAt = value;
  }

  /// <summary>
  /// Date when user completed this program
  /// </summary>
  public DateTime? CompletedAt {
    get => _completedAt;
    set => _completedAt = value;
  }

  /// <summary>
  /// Last date user accessed any content in this program
  /// </summary>
  public DateTime? LastAccessedAt {
    get => _lastAccessedAt;
    set => _lastAccessedAt = value;
  }

  public virtual ICollection<ContentInteraction> ContentInteractions {
    get => _contentInteractions;
    set => _contentInteractions = value;
  }

  public virtual ICollection<ActivityGrade> ReceivedGrades {
    get => _receivedGrades;
    set => _receivedGrades = value;
  }

  public virtual ICollection<ActivityGrade> GivenGrades {
    get => _givenGrades;
    set => _givenGrades = value;
  }

  public virtual ICollection<Certificate.Models.UserCertificate> UserCertificates {
    get => _userCertificates;
    set => _userCertificates = value;
  }

  public virtual ICollection<Feedback.Models.ProgramFeedbackSubmission> FeedbackSubmissions {
    get => _feedbackSubmissions;
    set => _feedbackSubmissions = value;
  }

  public virtual ICollection<Feedback.Models.ProgramRating> ProgramRatings {
    get => _programRatings;
    set => _programRatings = value;
  }

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

/// <summary>
/// Entity Framework configuration for ProgramUser entity
/// </summary>
public class ProgramUserConfiguration : IEntityTypeConfiguration<ProgramUser> {
  public void Configure(EntityTypeBuilder<ProgramUser> builder) {
    // Configure relationship with Program (can't be done with annotations)
    builder.HasOne(pu => pu.Program)
           .WithMany(p => p.ProgramUsers)
           .HasForeignKey(pu => pu.ProgramId)
           .OnDelete(DeleteBehavior.Cascade);

    // Configure relationship with User (can't be done with annotations)
    builder.HasOne(pu => pu.User).WithMany().HasForeignKey(pu => pu.UserId).OnDelete(DeleteBehavior.Cascade);
  }
}
