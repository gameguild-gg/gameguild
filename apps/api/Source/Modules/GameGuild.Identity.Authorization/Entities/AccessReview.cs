using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Represents an access review/certification campaign
///     Enables periodic review of user access rights to ensure compliance
/// </summary>
public class AccessReviewCampaign
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AccessReviewType ReviewType { get; set; }

    public AccessReviewScope Scope { get; set; }

    public string? ScopeFilter { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public AccessReviewStatus Status { get; set; } = AccessReviewStatus.Draft;

    public int TotalItems { get; set; }

    public int ReviewedItems { get; set; }

    public int ApprovedItems { get; set; }

    public int RevokedItems { get; set; }

    public Guid CreatedBy { get; set; }

    public Guid? CompletedBy { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool AutoRevokeOnNoResponse { get; set; }

    public int ReminderFrequencyDays { get; set; } = 7;

    public string? NotificationTemplate { get; set; }

    public DateTime CreatedAt { get; set; } = SystemClock.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<AccessReviewItem> Items { get; set; } = new List<AccessReviewItem>();

    /// <summary>
    ///     Check if campaign is open (started, still within its review window).
    ///     Both <see cref="AccessReviewStatus.Active"/> (opened, no decisions yet)
    ///     and <see cref="AccessReviewStatus.InProgress"/> (decisions recorded)
    ///     are open states (issue #1204).
    /// </summary>
    public bool IsActive() =>
        Status is AccessReviewStatus.Active or AccessReviewStatus.InProgress &&
        SystemClock.UtcNow >= StartDate &&
        SystemClock.UtcNow <= EndDate;

    /// <summary>
    ///     Check if campaign has expired (open past its end date without completion)
    /// </summary>
    public bool IsExpired() =>
        Status is AccessReviewStatus.Active or AccessReviewStatus.InProgress &&
        SystemClock.UtcNow > EndDate;

    /// <summary>
    ///     Calculate completion percentage
    /// </summary>
    public double GetCompletionPercentage()
    {
        if (TotalItems == 0)
        {
            return 0;
        }

        return (double)ReviewedItems / TotalItems * 100;
    }

    /// <summary>
    ///     Start the campaign (Draft → Active). <see cref="AccessReviewStatus.Active"/>
    ///     is the "opened for attestations" state; the campaign moves to
    ///     <see cref="AccessReviewStatus.InProgress"/> when the first item decision is
    ///     recorded (see <see cref="RecordDecision"/>) (issue #1204).
    /// </summary>
    public void Start()
    {
        if (Status != AccessReviewStatus.Draft)
        {
            throw new InvalidOperationException("Only draft campaigns can be started");
        }

        Status = AccessReviewStatus.Active;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Complete the campaign (from any open state: Active or InProgress)
    /// </summary>
    public void Complete(Guid completedByUserId)
    {
        if (Status is not (AccessReviewStatus.Active or AccessReviewStatus.InProgress))
        {
            throw new InvalidOperationException("Only open campaigns can be completed");
        }

        Status = AccessReviewStatus.Completed;
        CompletedBy = completedByUserId;
        CompletedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Cancel the campaign
    /// </summary>
    public void Cancel()
    {
        if (Status == AccessReviewStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed campaign");
        }

        Status = AccessReviewStatus.Expired; // Using Expired as cancelled state
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Mark campaign as expired
    /// </summary>
    public void MarkExpired()
    {
        Status = AccessReviewStatus.Expired;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Record one item decision: increments the reviewed and outcome counters and
    ///     moves an <see cref="AccessReviewStatus.Active"/> campaign to
    ///     <see cref="AccessReviewStatus.InProgress"/> (first response received).
    ///     Called exactly once per item by the decision paths (items can only be
    ///     decided from <see cref="AccessReviewItemStatus.Pending"/>, issue #1204).
    /// </summary>
    public void RecordDecision(bool approved)
    {
        if (Status == AccessReviewStatus.Active)
        {
            Status = AccessReviewStatus.InProgress;
        }

        IncrementReviewed();
        if (approved)
        {
            IncrementApproved();
        }
        else
        {
            IncrementRevoked();
        }
    }

    /// <summary>
    ///     Increment reviewed items counter
    /// </summary>
    public void IncrementReviewed()
    {
        ReviewedItems++;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Increment approved items counter
    /// </summary>
    public void IncrementApproved()
    {
        ApprovedItems++;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Increment revoked items counter
    /// </summary>
    public void IncrementRevoked()
    {
        RevokedItems++;
        UpdatedAt = SystemClock.UtcNow;
    }
}

/// <summary>
///     Individual item within an access review campaign
/// </summary>
public class AccessReviewItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CampaignId { get; set; }

    public Guid ReviewerId { get; set; }

    public Guid SubjectUserId { get; set; }

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string PermissionDetails { get; set; } = string.Empty;

    public AccessReviewItemStatus Status { get; set; } = AccessReviewItemStatus.Pending;

    public AccessReviewDecision? Decision { get; set; }

    public string? DecisionReason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? LastReminderSent { get; set; }

    public int ReminderCount { get; set; }

    public string? ReviewerNotes { get; set; }

    public DateTime CreatedAt { get; set; } = SystemClock.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AccessReviewCampaign Campaign { get; set; } = null!;

    /// <summary>
    ///     Check if item is pending review
    /// </summary>
    public bool IsPending() => Status == AccessReviewItemStatus.Pending;

    /// <summary>
    ///     Check if item needs reminder
    /// </summary>
    public bool NeedsReminder(int reminderFrequencyDays)
    {
        if (Status != AccessReviewItemStatus.Pending)
        {
            return false;
        }

        if (LastReminderSent == null)
        {
            return true;
        }

        return (SystemClock.UtcNow - LastReminderSent.Value).TotalDays >= reminderFrequencyDays;
    }

    /// <summary>
    ///     Approve the access. Only pending items can be decided — decisions never
    ///     overwrite each other (issue #1204).
    /// </summary>
    public void Approve(string? reason = null, string? notes = null)
    {
        EnsurePendingForDecision();
        Status = AccessReviewItemStatus.Approved;
        Decision = AccessReviewDecision.Approve;
        DecisionReason = reason;
        ReviewerNotes = notes;
        ReviewedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Revoke the access. Only pending items can be decided — decisions never
    ///     overwrite each other (issue #1204).
    /// </summary>
    public void Revoke(string reason, string? notes = null)
    {
        EnsurePendingForDecision();
        Status = AccessReviewItemStatus.Revoked;
        Decision = AccessReviewDecision.Revoke;
        DecisionReason = reason;
        ReviewerNotes = notes;
        ReviewedAt = SystemClock.UtcNow;
        UpdatedAt = SystemClock.UtcNow;
    }

    private void EnsurePendingForDecision()
    {
        if (Status != AccessReviewItemStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Review item {Id} has already been decided ({Status}); decisions cannot be overwritten.");
        }
    }

    /// <summary>
    ///     Record reminder sent
    /// </summary>
    public void RecordReminderSent()
    {
        LastReminderSent = SystemClock.UtcNow;
        ReminderCount++;
        UpdatedAt = SystemClock.UtcNow;
    }
}
