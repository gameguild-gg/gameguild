using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Tracks user progress through individual content items in a program
/// </summary>
[Table("content_progress")]
[Index(nameof(UserId), nameof(ContentId), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ContentId))]
[Index(nameof(CompletionStatus))]
[Index(nameof(CompletedAt))]
public class ContentProgress : Entity
{
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
    /// Foreign key to the Content entity
    /// </summary>
    [Required]
    public Guid ContentId { get; set; }

    /// <summary>
    /// Navigation property to the Content entity
    /// </summary>
    [ForeignKey(nameof(ContentId))]
    public virtual Content Content { get; set; } = null!;

    /// <summary>
    /// Program enrollment this progress belongs to
    /// </summary>
    [Required]
    public Guid ProgramEnrollmentId { get; set; }

    /// <summary>
    /// Navigation property to the Program Enrollment
    /// </summary>
    [ForeignKey(nameof(ProgramEnrollmentId))]
    public virtual ProgramEnrollment ProgramEnrollment { get; set; } = null!;

    /// <summary>
    /// Content completion status
    /// </summary>
    public ContentCompletionStatus CompletionStatus { get; set; } = ContentCompletionStatus.NotStarted;

    /// <summary>
    /// Progress percentage for this content item (0-100)
    /// </summary>
    [Range(0, 100)]
    public decimal ProgressPercentage { get; set; } = 0;

    /// <summary>
    /// When the user first accessed this content
    /// </summary>
    public DateTime? FirstAccessedAt { get; set; }

    /// <summary>
    /// When the user last accessed this content
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// When the content was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Time spent on this content (in seconds)
    /// </summary>
    public int TimeSpentSeconds { get; set; } = 0;

    /// <summary>
    /// Score/grade for this content (if applicable)
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// Maximum possible score for this content
    /// </summary>
    public decimal? MaxScore { get; set; }

    /// <summary>
    /// Number of attempts made on this content
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Additional progress data (JSON)
    /// For storing activity-specific progress data
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ProgressData { get; set; }

    /// <summary>
    /// Mark content as accessed
    /// </summary>
    public void MarkAsAccessed()
    {
        if (FirstAccessedAt == null)
        {
            FirstAccessedAt = DateTime.UtcNow;
            if (CompletionStatus == ContentCompletionStatus.NotStarted)
            {
                CompletionStatus = ContentCompletionStatus.InProgress;
            }
        }
        LastAccessedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>
    /// Mark content as completed
    /// </summary>
    public void MarkAsCompleted(decimal? score = null, decimal? maxScore = null)
    {
        CompletionStatus = ContentCompletionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        ProgressPercentage = 100;
        
        if (score.HasValue)
            Score = score.Value;
        if (maxScore.HasValue)
            MaxScore = maxScore.Value;
        
        MarkAsAccessed();
    }

    /// <summary>
    /// Update progress percentage
    /// </summary>
    public void UpdateProgress(decimal progressPercentage)
    {
        ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
        
        if (ProgressPercentage == 100 && CompletionStatus != ContentCompletionStatus.Completed)
        {
            CompletionStatus = ContentCompletionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }
        else if (ProgressPercentage > 0 && CompletionStatus == ContentCompletionStatus.NotStarted)
        {
            CompletionStatus = ContentCompletionStatus.InProgress;
        }
        
        MarkAsAccessed();
    }

    /// <summary>
    /// Add time spent on this content
    /// </summary>
    public void AddTimeSpent(int seconds)
    {
        TimeSpentSeconds += seconds;
        MarkAsAccessed();
    }

    /// <summary>
    /// Increment attempt counter
    /// </summary>
    public void IncrementAttempts()
    {
        Attempts++;
        MarkAsAccessed();
    }
}

/// <summary>
/// Content completion status enumeration
/// </summary>
public enum ContentCompletionStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Skipped = 3,
    Failed = 4,
    RequiresReview = 5
}
