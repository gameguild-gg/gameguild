namespace GameGuild.Authentication.Entities;

/// <summary>
///     Individual access review item within a campaign
/// </summary>
public abstract class AccessReviewItem : EntityBase<Guid>
{
    /// <summary>
    ///     Campaign this item belongs to
    /// </summary>
    public Guid CampaignId { get; set; }

    /// <summary>
    ///     Navigation property to campaign
    /// </summary>
    public virtual AccessReviewCampaign Campaign { get; set; } = null!;

    /// <summary>
    ///     User whose access is being reviewed
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    ///     Resource being reviewed (optional for tenant/content-type reviews)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    ///     Resource type name
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Content type name (for content-type level reviews)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     GameGuild.Permissions being reviewed (JSON serialized array)
    /// </summary>
    public string Permissions { get; set; } = string.Empty;

    /// <summary>
    ///     Assigned reviewer user ID
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    ///     Review decision
    /// </summary>
    public AccessReviewDecision? Decision { get; set; }

    /// <summary>
    ///     Reason for the decision
    /// </summary>
    public string? DecisionReason { get; set; }

    /// <summary>
    ///     Review completion timestamp
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    ///     Due date for this review
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    ///     Last reminder sent timestamp
    /// </summary>
    public DateTime? LastReminderSent { get; set; }

    /// <summary>
    ///     Number of reminders sent
    /// </summary>
    public int RemindersSent { get; set; }

    /// <summary>
    ///     Additional context information (JSON serialized)
    /// </summary>
    public string? ContextInfo { get; set; }

    /// <summary>
    ///     Make a review decision
    /// </summary>
    public void MakeDecision(AccessReviewDecision decision, Guid reviewerId, string? reason = null)
    {
        Decision = decision;
        ReviewerId = reviewerId;
        DecisionReason = reason;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if review is overdue
    /// </summary>
    public bool IsOverdue() { return DueDate.HasValue && DateTime.UtcNow > DueDate.Value && !Decision.HasValue; }

    /// <summary>
    ///     Check if review is pending
    /// </summary>
    public bool IsPending() { return !Decision.HasValue; }

    /// <summary>
    ///     Record reminder sent
    /// </summary>
    public void RecordReminderSent()
    {
        LastReminderSent = DateTime.UtcNow;
        RemindersSent++;
        UpdatedAt = DateTime.UtcNow;
    }
}
