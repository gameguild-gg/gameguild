namespace GameGuild.Authentication.Entities;

/// <summary>
///     Access review campaign for periodic permission auditing
/// </summary>
public abstract class AccessReviewCampaign : EntityBase<Guid>
{
    /// <summary>
    ///     Campaign name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Campaign description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Campaign status
    /// </summary>
    public AccessReviewCampaignStatus Status { get; set; } = AccessReviewCampaignStatus.Draft;

    /// <summary>
    ///     Campaign type (Quarterly, Annual, AdHoc, etc.)
    /// </summary>
    public AccessReviewCampaignType Type { get; set; } = AccessReviewCampaignType.AdHoc;

    /// <summary>
    ///     Scope of the review (Tenant, ContentType, Resource)
    /// </summary>
    public AccessReviewScope Scope { get; set; } = AccessReviewScope.Tenant;

    /// <summary>
    ///     Campaign start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    ///     Campaign end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    ///     Reminder frequency in days
    /// </summary>
    public int? ReminderFrequencyDays { get; set; }

    /// <summary>
    ///     Auto-revoke permissions when review expires without decision
    /// </summary>
    public bool AutoRevokeOnExpiry { get; set; } = false;

    /// <summary>
    ///     Grace period in days before auto-revocation
    /// </summary>
    public int? GracePeriodDays { get; set; }

    /// <summary>
    ///     User who created the campaign
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    ///     User who started the campaign
    /// </summary>
    public Guid? StartedBy { get; set; }

    /// <summary>
    ///     User who completed the campaign
    /// </summary>
    public Guid? CompletedBy { get; set; }

    /// <summary>
    ///     Campaign completion date
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     Target filter criteria (JSON serialized)
    /// </summary>
    public string? FilterCriteria { get; set; }

    /// <summary>
    ///     Campaign instructions for reviewers
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    ///     Collection of review items in this campaign
    /// </summary>
    public virtual ICollection<AccessReviewItem> ReviewItems { get; set; } = new List<AccessReviewItem>();

    /// <summary>
    ///     Start the campaign
    /// </summary>
    public void Start(Guid startedBy)
    {
        if (Status != AccessReviewCampaignStatus.Draft) throw new InvalidOperationException($"Cannot start campaign in {Status} status");

        Status = AccessReviewCampaignStatus.InProgress;
        StartedBy = startedBy;
        StartDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Complete the campaign
    /// </summary>
    public void Complete(Guid completedBy)
    {
        if (Status != AccessReviewCampaignStatus.InProgress) throw new InvalidOperationException($"Cannot complete campaign in {Status} status");

        Status = AccessReviewCampaignStatus.Completed;
        CompletedBy = completedBy;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if campaign is expired
    /// </summary>
    public bool IsExpired() { return EndDate.HasValue && DateTime.UtcNow > EndDate.Value; }

    /// <summary>
    ///     Check if campaign is active
    /// </summary>
    public bool IsActive() { return Status == AccessReviewCampaignStatus.InProgress && !IsExpired(); }
}
