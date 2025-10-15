using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Represents a user's enrollment in a program (course)
/// Tracks enrollment status, progress, and completion
/// </summary>
[Table("program_enrollments")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(EnrollmentStatus))]
[Index(nameof(CompletionStatus))]
public class ProgramEnrollment : Entity {
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
  /// Current enrollment status
  /// </summary>
  public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Active;

  /// <summary>
  /// How the user was enrolled (manual, auto from product purchase, etc.)
  /// </summary>
  public EnrollmentSource EnrollmentSource { get; set; } = EnrollmentSource.Manual;

  /// <summary>
  /// When the user was enrolled
  /// </summary>
  public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When enrollment started (for scheduled programs)
  /// </summary>
  public DateTime? StartDate { get; set; }

  /// <summary>
  /// When enrollment expires (if applicable)
  /// </summary>
  public DateTime? ExpiryDate { get; set; }

  /// <summary>
  /// Current completion status
  /// </summary>
  public CompletionStatus CompletionStatus { get; set; } = CompletionStatus.NotStarted;

  /// <summary>
  /// Progress percentage (0-100)
  /// </summary>
  [Range(0, 100)]
  public decimal ProgressPercentage { get; set; } = 0;

  /// <summary>
  /// When the program was completed
  /// </summary>
  public DateTime? CompletedAt { get; set; }

  /// <summary>
  /// Final grade/score (if applicable)
  /// </summary>
  public decimal? FinalGrade { get; set; }

  /// <summary>
  /// Whether a certificate was issued
  /// </summary>
  public bool CertificateIssued { get; set; } = false;

  /// <summary>
  /// When the certificate was issued
  /// </summary>
  public DateTime? CertificateIssuedAt { get; set; }

  /// <summary>
  /// ID of the issued certificate
  /// </summary>
  public Guid? CertificateId { get; set; }

  /// <summary>
  /// Additional metadata (JSON)
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? Metadata { get; set; }

  /// <summary>
  /// Calculate and update progress based on content completion
  /// </summary>
  public void UpdateProgress() {
    // This will be implemented in the service layer
    Touch();
  }

  /// <summary>
  /// Mark as completed and issue certificate if eligible
  /// </summary>
  public void MarkAsCompleted(decimal? finalGrade = null) {
    CompletionStatus = CompletionStatus.Completed;
    CompletedAt = DateTime.UtcNow;
    FinalGrade = finalGrade;
    ProgressPercentage = 100;
    Touch();
  }
}

/// <summary>
/// Enrollment status enumeration
/// </summary>
public enum EnrollmentStatus {
  Open = 0,
  Active = 1,
  Paused = 2,
  Cancelled = 3,
  Expired = 4,
  Completed = 5,
}

/// <summary>
/// How the user was enrolled
/// </summary>
public enum EnrollmentSource {
  Manual = 0,
  ProductPurchase = 1,
  FreeAccess = 2,
  AdminAction = 3,
  BulkEnrollment = 4,
  Invitation = 5,
}

/// <summary>
/// Completion status enumeration
/// </summary>
public enum CompletionStatus {
  NotStarted = 0,
  InProgress = 1,
  Completed = 2,
  CompletedWithCertificate = 3,
  Failed = 4,
  Dropped = 5,
}
