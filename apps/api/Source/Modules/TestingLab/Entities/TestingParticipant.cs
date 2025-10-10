using GameGuild.Domain.Common;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.TestingLab.Entities;

/// <summary>
/// Represents a participant in testing sessions and QA activities
/// </summary>
[Table("testing_participants")]
[Index(nameof(TestingRequestId))]
[Index(nameof(UserId))]
[Index(nameof(InstructionsAcknowledged))]
[Index(nameof(StartedAt))]
[Index(nameof(CompletedAt))]
[Index(nameof(TenantId))]
public class TestingParticipant : EntityBase
{
    /// <summary>
    /// Foreign key to the testing request
    /// </summary>
    [Required]
    public Guid TestingRequestId { get; set; }

    /// <summary>
    /// Navigation property to the testing request
    /// </summary>
    public virtual TestingRequest TestingRequest { get; set; } = null!;

    /// <summary>
    /// Foreign key to the user
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Whether instructions have been acknowledged
    /// </summary>
    [Required]
    public bool InstructionsAcknowledged { get; set; } = false;

    /// <summary>
    /// When instructions were acknowledged
    /// </summary>
    public DateTime? InstructionsAcknowledgedAt { get; set; }

    /// <summary>
    /// When participation started
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When participation was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Time spent testing in minutes
    /// </summary>
    public int? TimeSpentMinutes { get; set; }

    /// <summary>
    /// Number of feedback submissions
    /// </summary>
    public int FeedbackCount { get; set; } = 0;

    /// <summary>
    /// Participation status
    /// </summary>
    public ParticipationStatus Status { get; set; } = ParticipationStatus.Registered;

    /// <summary>
    /// Notes about the participation
    /// </summary>
    public string? Notes { get; set; }

    // Navigation Properties
    /// <summary>
    /// Feedback provided by this participant
    /// </summary>
    public virtual ICollection<TestingFeedback> Feedback { get; set; } = new List<TestingFeedback>();

    // Computed Properties
    /// <summary>
    /// Whether this participant is global (tenant-independent)
    /// </summary>
    public bool IsGlobal => TenantId == null;

    /// <summary>
    /// Whether participation is active
    /// </summary>
    public bool IsActive => Status == ParticipationStatus.Active;

    /// <summary>
    /// Whether participation is completed
    /// </summary>
    public bool IsCompleted => Status == ParticipationStatus.Completed;

    /// <summary>
    /// Duration of participation
    /// </summary>
    public TimeSpan? ParticipationDuration => CompletedAt.HasValue && StartedAt != default
        ? CompletedAt.Value - StartedAt
        : null;

    /// <summary>
    /// Whether participant can provide feedback
    /// </summary>
    public bool CanProvideFeedback => InstructionsAcknowledged && IsActive;

    // Domain Methods
    /// <summary>
    /// Acknowledges instructions
    /// </summary>
    public void AcknowledgeInstructions()
    {
        InstructionsAcknowledged = true;
        InstructionsAcknowledgedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Starts active participation
    /// </summary>
    public void Start()
    {
        if (!InstructionsAcknowledged)
            throw new InvalidOperationException("Instructions must be acknowledged before starting");

        Status = ParticipationStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Completes participation
    /// </summary>
    public void Complete()
    {
        Status = ParticipationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Withdraws from participation
    /// </summary>
    public void Withdraw()
    {
        Status = ParticipationStatus.Withdrawn;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records time spent testing
    /// </summary>
    public void RecordTimeSpent(int minutes)
    {
        TimeSpentMinutes = (TimeSpentMinutes ?? 0) + minutes;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments feedback count
    /// </summary>
    public void IncrementFeedbackCount()
    {
        FeedbackCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates participation notes
    /// </summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Participation status enumeration
/// </summary>
public enum ParticipationStatus
{
    Registered = 0,
    Active = 1,
    Completed = 2,
    Withdrawn = 3
}