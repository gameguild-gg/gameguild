using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameGuild.Learning.Courses;

/// <summary>
/// Represents a grade assigned to a user for a specific activity or content interaction
/// </summary>
[Table("activity_grades")]
[Index(nameof(StudentId), nameof(ContentInteractionId), IsUnique = true)]
[Index(nameof(StudentId))]
[Index(nameof(GraderId))]
[Index(nameof(ContentInteractionId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(GradedAt))]
[Index(nameof(Points))]
[Index(nameof(TenantId))]
public class ActivityGrade : EntityBase
{
    /// <summary>
    /// Student user ID
    /// </summary>
    [Required]
    public Guid StudentId { get; set; }

    /// <summary>
    /// Grader user ID (instructor, peer, or system)
    /// </summary>
    [Required]
    public Guid GraderId { get; set; }

    /// <summary>
    /// Content interaction being graded
    /// </summary>
    [Required]
    public Guid ContentInteractionId { get; set; }

    /// <summary>
    /// Program enrollment ID
    /// </summary>
    [Required]
    public Guid ProgramUserId { get; set; }

    /// <summary>
    /// Points awarded (0-100 scale)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
        public decimal? Points { get; set; }

        /// <summary>
        /// Compatibility shim for legacy services referencing Grade property
        /// </summary>
        [NotMapped]
        public decimal Grade
        {
            get => Points ?? 0m;
            set => Points = value;
        }
    /// <summary>
    /// Maximum points possible for this activity
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxPoints { get; set; }

    /// <summary>
    /// Grade letter/symbol (A, B, C, etc.)
    /// </summary>
    [MaxLength(10)]
    public string? GradeLetter { get; set; }

    /// <summary>
    /// Grader feedback/comments
    /// </summary>
    public string? Feedback { get; set; }

    /// <summary>
    /// When the grade was assigned
    /// </summary>
    public DateTime GradedAt { get; set; }

    /// <summary>
    /// Whether this grade is finalized
    /// </summary>
    public bool IsFinalized { get; set; } = false;

    /// <summary>
    /// Rubric data (JSON format)
    /// </summary>
    public string? RubricData { get; set; }

    /// <summary>
    /// Time spent grading in minutes
    /// </summary>
    public int? GradingTimeMinutes { get; set; }

    /// <summary>
    /// Grade type (automatic, manual, peer review)
    /// </summary>
    public GradeType GradeType { get; set; } = GradeType.Manual;

    /// <summary>
    /// Attempt number for this activity
    /// </summary>
    public int AttemptNumber { get; set; } = 1;

    // Navigation Properties
    /// <summary>
    /// Student user
    /// </summary>
    public virtual User Student { get; set; } = null!;

    /// <summary>
    /// Grader user
    /// </summary>
    public virtual User Grader { get; set; } = null!;

    /// <summary>
    /// Content interaction
    /// </summary>
    public virtual ContentInteraction ContentInteraction { get; set; } = null!;

    /// <summary>
    /// Program enrollment
    /// </summary>
    public virtual ProgramUser ProgramUser { get; set; } = null!;

    /// <summary>
    /// Grader's program user record (for tracking which program user did the grading)
    /// </summary>
    public virtual ProgramUser? GraderProgramUser { get; set; }

    /// <summary>
    /// Grader program user ID
    /// </summary>
    public Guid? GraderProgramUserId { get; set; }

    /// <summary>
    /// Detailed grading information (JSON format)
    /// </summary>
    public string? GradingDetails { get; set; }

    // Computed Properties
    /// <summary>
    /// Whether this grade is global (tenant-independent)
    /// </summary>
    public override bool IsGlobal => TenantId == null;

    /// <summary>
    /// Percentage score (points / max points * 100)
    /// </summary>
    public decimal? PercentageScore => Points.HasValue && MaxPoints.HasValue && MaxPoints > 0
        ? (Points.Value / MaxPoints.Value) * 100m
        : null;

    /// <summary>
    /// Whether this is a passing grade (>=60%)
    /// </summary>
    public bool? IsPassing => PercentageScore.HasValue ? PercentageScore >= 60m : null;

    /// <summary>
    /// Whether this is an automatic system grade
    /// </summary>
    public bool IsAutomaticGrade => GradeType == GradeType.Automatic;

    /// <summary>
    /// Whether this is a peer review grade
    /// </summary>
    public bool IsPeerReview => GradeType == GradeType.PeerReview;

    /// <summary>
    /// Days since grading
    /// </summary>
    public int DaysSinceGrading => (SystemClock.UtcNow - GradedAt).Days;

    // Domain Methods
    /// <summary>
    /// Assigns points to this grade
    /// </summary>
    public void AssignPoints(decimal points, decimal? maxPoints = null)
    {
        Points = Math.Max(0, Math.Min(maxPoints ?? 100m, points));
        MaxPoints = maxPoints ?? MaxPoints ?? 100m;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Sets the letter grade
    /// </summary>
    public void SetLetterGrade(string grade)
    {
        GradeLetter = grade?.ToUpper();
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Adds or updates feedback
    /// </summary>
    public void UpdateFeedback(string? feedback)
    {
        Feedback = feedback;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Finalizes the grade (prevents further changes)
    /// </summary>
    public void FinalizeGrade()
    {
        IsFinalized = true;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Unlocks the grade for editing
    /// </summary>
    public void Unlock()
    {
        IsFinalized = false;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Records grading time
    /// </summary>
    public void RecordGradingTime(int minutes)
    {
        GradingTimeMinutes = minutes;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Sets rubric evaluation data
    /// </summary>
    public void SetRubricData(string rubricJson)
    {
        RubricData = rubricJson;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Calculates letter grade from percentage
    /// </summary>
    public string? CalculateLetterGrade()
    {
        if (!PercentageScore.HasValue)
            return null;

        return PercentageScore.Value switch
        {
            >= 97m => "A+",
            >= 93m => "A",
            >= 90m => "A-",
            >= 87m => "B+",
            >= 83m => "B",
            >= 80m => "B-",
            >= 77m => "C+",
            >= 73m => "C",
            >= 70m => "C-",
            >= 67m => "D+",
            >= 63m => "D",
            >= 60m => "D-",
            _ => "F"
        };
    }

    /// <summary>
    /// Validates the grade data
    /// </summary>
    public bool IsValid()
    {
        if (Points.HasValue && MaxPoints.HasValue && Points > MaxPoints)
            return false;

        if (Points.HasValue && Points < 0)
            return false;

        if (MaxPoints is <= 0)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a revision of this grade
    /// </summary>
    public ActivityGrade CreateRevision(decimal newPoints, string? reason = null)
    {
        return new ActivityGrade
        {
            StudentId = StudentId,
            GraderId = GraderId,
            ContentInteractionId = ContentInteractionId,
            ProgramUserId = ProgramUserId,
            Points = newPoints,
            MaxPoints = MaxPoints,
            GradeLetter = CalculateLetterGrade(),
            Feedback = reason != null ? $"Revision: {reason}\\n\\nOriginal feedback: {Feedback}" : Feedback,
            GradedAt = SystemClock.UtcNow,
            IsFinalized = false,
            GradeType = GradeType,
            AttemptNumber = AttemptNumber + 1,
            TenantId = TenantId
        };
    }
}
