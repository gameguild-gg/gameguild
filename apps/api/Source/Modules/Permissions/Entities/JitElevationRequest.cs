namespace GameGuild.Modules.Permissions;

/// <summary>
/// Entity for Just-in-Time (JIT) permission elevation requests
/// Enables time-bound temporary permission grants with approval workflow
/// </summary>
[Table("JitElevationRequests")]
[Index(nameof(RequesterId), Name = "IX_JitElevationRequests_RequesterId")]
[Index(nameof(TenantId), Name = "IX_JitElevationRequests_TenantId")]
[Index(nameof(Status), Name = "IX_JitElevationRequests_Status")]
[Index(nameof(ExpiresAt), Name = "IX_JitElevationRequests_ExpiresAt")]
public class JitElevationRequest : EntityBase
{
    /// <summary>
    /// User requesting the elevation
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// Navigation property to requester
    /// </summary>
    [GraphQLIgnore]
    public virtual User? Requester { get; set; }

    /// <summary>
    /// Tenant context for the request
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to tenant
    /// </summary>
    [GraphQLIgnore]
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Permission type being requested
    /// </summary>
    public PermissionType Permission { get; set; }

    /// <summary>
    /// Resource type (if resource-specific)
    /// </summary>
    [MaxLength(100)]
    public string? ResourceType { get; set; }

    /// <summary>
    /// Specific resource ID (if resource-specific)
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    /// Business justification for the request
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// Duration of the elevation in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// When the elevation starts (null for immediate)
    /// </summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>
    /// When the elevation expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Request status
    /// </summary>
    public ElevationRequestStatus Status { get; set; } = ElevationRequestStatus.Pending;

    /// <summary>
    /// ID of user who approved/denied the request
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Navigation property to reviewer
    /// </summary>
    [GraphQLIgnore]
    public virtual User? Reviewer { get; set; }

    /// <summary>
    /// When the request was reviewed
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Reviewer's comments
    /// </summary>
    [MaxLength(1000)]
    public string? ReviewerComments { get; set; }

    /// <summary>
    /// When the permission was actually granted
    /// </summary>
    public DateTime? GrantedAt { get; set; }

    /// <summary>
    /// When the permission was revoked (if auto-revoked)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// ID of the tenant permission that was granted
    /// </summary>
    public Guid? GrantedPermissionId { get; set; }

    /// <summary>
    /// Whether auto-revocation is enabled
    /// </summary>
    public bool AutoRevoke { get; set; } = true;

    /// <summary>
    /// Escalation level (0=normal, 1=escalated, 2=critical)
    /// </summary>
    public int EscalationLevel { get; set; } = 0;

    /// <summary>
    /// Whether this request requires approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Priority of the request (0=low, 1=normal, 2=high, 3=urgent)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Check if the elevation is currently active
    /// </summary>
    [GraphQLIgnore]
    public bool IsActive =>
        Status == ElevationRequestStatus.Granted &&
        DateTime.UtcNow >= (StartsAt ?? DateTime.MinValue) &&
        DateTime.UtcNow < ExpiresAt &&
        !IsDeleted;

    /// <summary>
    /// Check if the elevation has expired
    /// </summary>
    [GraphQLIgnore]
    public bool IsExpired =>
        Status == ElevationRequestStatus.Granted &&
        DateTime.UtcNow >= ExpiresAt;
}

/// <summary>
/// Status of a JIT elevation request
/// </summary>
public enum ElevationRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Granted = 3,
    Expired = 4,
    Revoked = 5,
    Cancelled = 6
}
