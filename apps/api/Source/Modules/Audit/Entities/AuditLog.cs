namespace GameGuild.Modules.Audit;

/// <summary>
/// Audit log entry for tracking security-sensitive operations
/// </summary>
public class AuditLog : EntityBase
{
    /// <summary>
    /// Type of action being audited
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Resource type being acted upon (User, Permission, Role, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier of the resource
    /// </summary>
    [MaxLength(100)]
    public string? ResourceId { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tenant context for the action
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Session ID associated with the action
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Detailed description of the action
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Risk level of the action
    /// </summary>
    public AuditRiskLevel RiskLevel { get; set; } = AuditRiskLevel.Low;

    /// <summary>
    /// Category of the audit event
    /// </summary>
    public AuditCategory Category { get; set; } = AuditCategory.General;

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}
