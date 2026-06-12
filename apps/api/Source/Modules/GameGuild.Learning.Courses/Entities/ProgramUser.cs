using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Represents a user's enrollment in a learning program with progress tracking and completion status
/// </summary>
[Table("program_users")]
[Index(nameof(UserId), nameof(ProgramId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ProgramId))]
[Index(nameof(IsActive))]
[Index(nameof(JoinedAt))]
[Index(nameof(CompletedAt))]
[Index(nameof(TenantId))]
public class ProgramUser : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Program ID
    /// </summary>
    [Required]
    public Guid ProgramId { get; set; }

    /// <summary>
    /// Whether the enrollment is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when user joined the program
    /// </summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// Current completion percentage (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal CompletionPercentage { get; set; } = 0m;

    /// <summary>
    /// Final grade for the program (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? FinalGrade { get; set; }

    /// <summary>
    /// Date when user started the program content
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Date when user completed the program
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Last access date
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Enrolled user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Program
    /// </summary>
    public virtual Program Program { get; set; } = null!;

    /// <summary>
    /// Content interactions (progress tracking)
    /// </summary>
    public virtual ICollection<ContentInteraction> ContentInteractions { get; set; } = new List<ContentInteraction>();

    /// <summary>
    /// Grades received by this user
    /// </summary>
    public virtual ICollection<ActivityGrade> ReceivedGrades { get; set; } = new List<ActivityGrade>();

    /// <summary>
    /// Grades given by this user (if they're an instructor/peer)
    /// </summary>
    public virtual ICollection<ActivityGrade> GivenGrades { get; set; } = new List<ActivityGrade>();

    // Certificates are issued through ProgramEnrollmentService via ICertificateIssuanceService
    // to avoid a circular entity dependency from Courses back into Learning.Certificates.

    /// <summary>
    /// Program ratings and written reviews submitted by this enrollment.
    /// </summary>
    public virtual ICollection<ProgramRating> ProgramRatings { get; set; } = new List<ProgramRating>();

    // Computed Properties
    /// <summary>
    /// Whether this enrollment is global (tenant-independent)
    /// </summary>
    public override bool IsGlobal => TenantId == null;

    /// <summary>
    /// Whether the program is completed
    /// </summary>
    public bool IsCompleted => CompletedAt.HasValue;

    /// <summary>
    /// Whether the program is in progress
    /// </summary>
    public bool IsInProgress => StartedAt.HasValue && !CompletedAt.HasValue && IsActive;

    /// <summary>
    /// Days since enrollment
    /// </summary>
    public int DaysSinceEnrollment => (SystemClock.UtcNow - JoinedAt).Days;

    /// <summary>
    /// Days since last access
    /// </summary>
    public int? DaysSinceLastAccess => LastAccessedAt.HasValue
        ? (SystemClock.UtcNow - LastAccessedAt.Value).Days
        : null;

    /// <summary>
    /// Average grade across all activities
    /// </summary>
    public decimal? AverageGrade
    {
        get
        {
            if (ReceivedGrades == null)
                return null;

            var grades = ReceivedGrades.Where(g => g.Points.HasValue).Select(g => g.Points!.Value).ToList();
            return grades.Count > 0 ? grades.Average() : null;
        }
    }

    // Domain Methods
    /// <summary>
    /// Starts the program (first content access)
    /// </summary>
    public void Start()
    {
        if (!StartedAt.HasValue)
        {
            StartedAt = SystemClock.UtcNow;
        }
        UpdateLastAccess();
    }

    /// <summary>
    /// Completes the program
    /// </summary>
    public void Complete(decimal? finalGrade = null)
    {
        if (CompletedAt.HasValue)
            return; // Already completed

        CompletedAt = SystemClock.UtcNow;
        CompletionPercentage = 100m;
        FinalGrade = finalGrade ?? CalculateFinalGrade();
        UpdateLastAccess();
    }

    /// <summary>
    /// Updates last access timestamp
    /// </summary>
    public void UpdateLastAccess()
    {
        LastAccessedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Deactivates the enrollment (dropout)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Reactivates the enrollment
    /// </summary>
    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Updates completion percentage based on content interactions
    /// </summary>
    public void UpdateCompletionPercentage()
    {
        var programContents = Program.ProgramContents?.Where(pc => pc.IsRequired).ToList();
        if (programContents?.Any() != true)
        {
            CompletionPercentage = 0m;
            return;
        }

        var completedCount = programContents.Count(pc =>
            ContentInteractions.Any(ci => ci.ContentId == pc.Id && ci.IsCompleted));

        CompletionPercentage = (decimal)completedCount / programContents.Count * 100m;

        // Auto-complete if all required content is done
        if (CompletionPercentage >= 100m && !CompletedAt.HasValue)
        {
            Complete();
        }

        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    /// Calculates final grade based on all activity grades
    /// </summary>
    public decimal? CalculateFinalGrade()
    {
        var grades = ReceivedGrades?.Where(g => g.Points.HasValue).ToList();
        if (grades?.Any() != true)
            return null;

        // Weighted average could be implemented here based on activity importance
        return grades.Average(g => g.Points!.Value);
    }

    /// <summary>
    /// Checks if user can access specific content
    /// </summary>
    public bool CanAccessContent(Guid contentId)
    {
        if (!IsActive)
            return false;

        var content = Program.ProgramContents?.FirstOrDefault(pc => pc.Id == contentId);
        if (content == null)
            return false;

        // Basic access check - can be extended with prerequisites logic
        return content.IsAccessibleBy(UserId);
    }

    /// <summary>
    /// Gets progress for a specific content item
    /// </summary>
    public ContentInteraction? GetContentProgress(Guid contentId)
    {
        return ContentInteractions?.FirstOrDefault(ci => ci.ContentId == contentId);
    }
}
