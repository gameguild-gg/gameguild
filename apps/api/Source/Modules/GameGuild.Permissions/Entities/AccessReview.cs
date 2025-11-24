using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Entities;

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<AccessReviewItem> Items { get; set; } = new List<AccessReviewItem>();

    /// <summary>
    ///     Check if campaign is active and in progress
    /// </summary>
    public bool IsActive() { return Status == AccessReviewStatus.InProgress && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate; }

    /// <summary>
    ///     Check if campaign has expired
    /// </summary>
    public bool IsExpired() { return Status == AccessReviewStatus.InProgress && DateTime.UtcNow > EndDate; }

    /// <summary>
    ///     Calculate completion percentage
    /// </summary>
    public double GetCompletionPercentage()
    {
        if (TotalItems == 0) return 0;

        return (double) ReviewedItems / TotalItems * 100;
    }

    /// <summary>
    ///     Start the campaign (change status from Draft to InProgress)
    /// </summary>
    public void Start()
    {
        if (Status != AccessReviewStatus.Draft) throw new InvalidOperationException("Only draft campaigns can be started");

        Status = AccessReviewStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Complete the campaign
    /// </summary>
    public void Complete(Guid completedByUserId)
    {
        if (Status != AccessReviewStatus.InProgress) throw new InvalidOperationException("Only in-progress campaigns can be completed");

        Status = AccessReviewStatus.Completed;
        CompletedBy = completedByUserId;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Cancel the campaign
    /// </summary>
    public void Cancel()
    {
        if (Status == AccessReviewStatus.Completed) throw new InvalidOperationException("Cannot cancel a completed campaign");

        Status = AccessReviewStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mark campaign as expired
    /// </summary>
    public void MarkExpired()
    {
        Status = AccessReviewStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Increment reviewed items counter
    /// </summary>
    public void IncrementReviewed()
    {
        ReviewedItems++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Increment approved items counter
    /// </summary>
    public void IncrementApproved()
    {
        ApprovedItems++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Increment revoked items counter
    /// </summary>
    public void IncrementRevoked()
    {
        RevokedItems++;
        UpdatedAt = DateTime.UtcNow;
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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public AccessReviewCampaign Campaign { get; set; } = null!;

    /// <summary>
    ///     Check if item is pending review
    /// </summary>
    public bool IsPending() { return Status == AccessReviewItemStatus.Pending; }

    /// <summary>
    ///     Check if item needs reminder
    /// </summary>
    public bool NeedsReminder(int reminderFrequencyDays)
    {
        if (Status != AccessReviewItemStatus.Pending) return false;
        if (LastReminderSent == null) return true;

        return DateTime.UtcNow >= LastReminderSent.Value.AddDays(reminderFrequencyDays);
    }

    /// <summary>
    ///     Review the item (approve or revoke)
    /// </summary>
    public void Review(Guid reviewerId, AccessReviewDecision decision, string? reason = null)
    {
        if (Status != AccessReviewItemStatus.Pending) throw new InvalidOperationException("Only pending items can be reviewed");

        ReviewerId = reviewerId;
        Decision = decision;
        DecisionReason = reason;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Status = decision switch
        {
            AccessReviewDecision.Approve => AccessReviewItemStatus.Approved,
            AccessReviewDecision.Revoke => AccessReviewItemStatus.Revoked,
            AccessReviewDecision.ModifyAndApprove => AccessReviewItemStatus.Approved,
            _ => throw new ArgumentException("Invalid decision", nameof(decision))
        };
    }

    /// <summary>
    ///     Record that a reminder was sent
    /// </summary>
    public void RecordReminderSent()
    {
        LastReminderSent = DateTime.UtcNow;
        ReminderCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Mark as expired
    /// </summary>
    public void MarkExpired()
    {
        Status = AccessReviewItemStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }
}
