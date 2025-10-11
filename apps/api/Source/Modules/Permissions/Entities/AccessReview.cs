namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents an access review/certification campaign
/// </summary>
public class AccessReviewCampaign : EntityBase<Guid>
{
    // TenantId inherited from EntityBase (no override needed)
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccessReviewType ReviewType { get; set; }
    public AccessReviewScope Scope { get; set; }
    public string? ScopeFilter { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public AccessReviewStatus Status { get; set; }
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
    public ICollection<AccessReviewItem> Items { get; set; } = new List<AccessReviewItem>();
}

/// <summary>
/// Individual item within an access review campaign
/// </summary>
public class AccessReviewItem : EntityBase<Guid>
{
    public Guid CampaignId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid SubjectUserId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string PermissionDetails { get; set; } = string.Empty;
    public AccessReviewItemStatus Status { get; set; }
    public AccessReviewDecision? Decision { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? LastReminderSent { get; set; }
    public int ReminderCount { get; set; }
    public string? ReviewerNotes { get; set; }
    public AccessReviewCampaign Campaign { get; set; } = null!;
}

public enum AccessReviewType
{
    PermissionReview = 1,
    RoleReview = 2,
    ResourceAccessReview = 3,
    UserAccessReview = 4,
    ComplianceAttestation = 5
}

public enum AccessReviewScope
{
    AllUsers = 1,
    Department = 2,
    Team = 3,
    Role = 4,
    Resource = 5,
    HighPrivilege = 6,
    External = 7,
    Custom = 99
}

public enum AccessReviewStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}

public enum AccessReviewItemStatus
{
    Pending = 1,
    Reviewed = 2,
    Escalated = 3,
    AutoApproved = 4,
    AutoRevoked = 5
}

public enum AccessReviewDecision
{
    Approve = 1,
    Revoke = 2,
    Modify = 3,
    Escalate = 4
}
