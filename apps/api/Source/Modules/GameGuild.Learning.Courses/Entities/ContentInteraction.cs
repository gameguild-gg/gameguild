using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GameGuild.Learning.Courses;

/// <summary>
/// Represents a user's interaction with program content, tracking progress and completion
/// </summary>
[Table("content_interactions")]
[Index(nameof(UserId), nameof(ContentId))]
[Index(nameof(UserId))]
[Index(nameof(ContentId))]
[Index(nameof(ProgramUserId))]
[Index(nameof(IsCompleted))]
[Index(nameof(StartedAt))]
[Index(nameof(CompletedAt))]
[Index(nameof(TenantId))]
public class ContentInteraction : EntityBase
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Content ID
    /// </summary>
    [Required]
    public Guid ContentId { get; set; }

    /// <summary>
    /// Program enrollment ID
    /// </summary>
    [Required]
    public Guid ProgramUserId { get; set; }

    /// <summary>
    /// Whether the content has been completed
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ProgressPercentage { get; set; } = 0m;

    /// <summary>
    /// Time spent on this content in minutes
    /// </summary>
    public int? TimeSpentMinutes { get; set; } = 0;

    /// <summary>
    /// Exact accumulated active time. TimeSpentMinutes remains as a compatibility projection.
    /// </summary>
    public int TimeSpentSeconds { get; set; }

    /// <summary>
    /// When the user started this content
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the user completed this content
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Current status of the interaction
    /// </summary>
    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;

    /// <summary>
    /// When the user submitted their work (for assignments)
    /// </summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>
    /// First time the user accessed this content
    /// </summary>
    public DateTime? FirstAccessedAt { get; set; }

    /// <summary>
    /// Last access timestamp
    /// </summary>
    public DateTime? LastAccessedAt { get; set; }

    /// <summary>
    /// Completion percentage (0-100) - alias for ProgressPercentage
    /// </summary>
    public decimal CompletionPercentage
    {
        get => ProgressPercentage ?? 0m;
        set => ProgressPercentage = value;
    }

    /// <summary>
    /// Submission data (JSON format for assignments/activities)
    /// </summary>
    public string? SubmissionData { get; set; }

    /// <summary>
    /// Number of attempts (for quizzes/assignments)
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Best score achieved
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? BestScore { get; set; }

    /// <summary>
    /// User notes/annotations for this content
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Bookmarked position (for video/text content)
    /// </summary>
    public string? BookmarkPosition { get; set; }

    // Navigation Properties
    /// <summary>
    /// User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Program content
    /// </summary>
    public virtual ProgramContent Content { get; set; } = null!;

    /// <summary>
    /// Program enrollment
    /// </summary>
    public virtual ProgramUser ProgramUser { get; set; } = null!;

    /// <summary>
    /// Activity grades for this interaction
    /// </summary>
    public virtual ICollection<ActivityGrade> ActivityGrades { get; set; } = new List<ActivityGrade>();

    /// <summary>
    /// Fine-grained lesson and video interaction timeline.
    /// </summary>
    public virtual ICollection<ContentInteractionEvent> Events { get; set; } = new List<ContentInteractionEvent>();

    // Computed Properties
    /// <summary>
    /// Whether this interaction is global (tenant-independent)
    /// </summary>
    public override bool IsGlobal => TenantId == null;

    /// <summary>
    /// Whether the content has been started
    /// </summary>
    public bool IsStarted => StartedAt.HasValue;

    /// <summary>
    /// Whether the content is in progress
    /// </summary>
    public bool IsInProgress => IsStarted && !IsCompleted;

    /// <summary>
    /// Time since last access in days
    /// </summary>
    public int? DaysSinceLastAccess => LastAccessedAt.HasValue
        ? (SystemClock.UtcNow - LastAccessedAt.Value).Days
        : null;

    /// <summary>
    /// Duration of engagement (completed - started)
    /// </summary>
    public TimeSpan? EngagementDuration => StartedAt.HasValue && CompletedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;

    // Domain Methods
    /// <summary>
    /// Starts the content interaction
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
    /// Updates progress percentage
    /// </summary>
    public void UpdateProgress(decimal percentage)
    {
        if (IsCompleted)
        {
            Status = ProgressStatus.Completed;
            ProgressPercentage = 100m;
            UpdateLastAccess();
            return;
        }

        ProgressPercentage = Math.Max(0, Math.Min(100, percentage));

        // Auto-complete if 100%
        if (ProgressPercentage >= 100 && !IsCompleted)
        {
            Complete();
        }

        UpdateLastAccess();
    }

    /// <summary>
    /// Completes the content interaction
    /// </summary>
    public void Complete()
    {
        IsCompleted = true;
        Status = ProgressStatus.Completed;
        CompletedAt ??= SystemClock.UtcNow;
        ProgressPercentage = 100m;
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
    /// Records time spent on content
    /// </summary>
    public void AddTimeSpent(int minutes)
    {
        if (minutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Time spent must be positive.");
        }

        AddTimeSpentSeconds(checked(minutes * 60));
    }

    /// <summary>
    /// Records exact active time while maintaining the legacy whole-minute value.
    /// </summary>
    public void AddTimeSpentSeconds(int seconds)
    {
        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Time spent must be positive.");
        }

        TimeSpentSeconds = checked(TimeSpentSeconds + seconds);
        TimeSpentMinutes = TimeSpentSeconds / 60;
        UpdateLastAccess();
    }

    /// <summary>
    /// Increments attempt count (for assessments)
    /// </summary>
    public void RecordAttempt(decimal? score = null)
    {
        AttemptCount++;

        if (score.HasValue && (BestScore == null || score > BestScore))
        {
            BestScore = score;
        }

        UpdateLastAccess();
    }

    /// <summary>
    /// Sets a bookmark position
    /// </summary>
    public void SetBookmark(string position)
    {
        BookmarkPosition = position;
        UpdateLastAccess();
    }

    /// <summary>
    /// Adds or updates user notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateLastAccess();
    }

    /// <summary>
    /// Resets progress (for retaking content)
    /// </summary>
    public void Reset()
    {
        IsCompleted = false;
        CompletedAt = null;
        ProgressPercentage = 0m;
        TimeSpentMinutes = 0;
        TimeSpentSeconds = 0;
        AttemptCount = 0;
        BestScore = null;
        BookmarkPosition = null;
        UpdateLastAccess();
    }

    /// <summary>
    /// Calculates engagement quality score
    /// </summary>
    public decimal CalculateEngagementScore()
    {
        var score = 0m;

        // Progress contribution (0-40 points)
        score += (ProgressPercentage ?? 0m) * 0.4m;

        // Completion bonus (20 points)
        if (IsCompleted)
            score += 20m;

        // Time engagement (0-20 points based on reasonable time spent)
        if (TimeSpentMinutes.HasValue && Content?.EstimatedMinutes.HasValue == true)
        {
            var timeRatio = (decimal)TimeSpentMinutes.Value / Content.EstimatedMinutes.Value;
            // Optimal engagement is 0.5-2x estimated time
            if (timeRatio >= 0.5m && timeRatio <= 2m)
                score += 20m;
            else if (timeRatio > 0.2m)
                score += 10m;
        }

        // Attempt efficiency (0-20 points for assessments)
        if (AttemptCount > 0 && BestScore.HasValue)
        {
            var efficiency = BestScore.Value / AttemptCount;
            score += Math.Min(20m, efficiency * 0.2m);
        }

        return Math.Min(100m, score);
    }
}
