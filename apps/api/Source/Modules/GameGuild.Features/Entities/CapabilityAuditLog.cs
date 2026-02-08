using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
/// Audit log for tracking capability changes. Every modification to tenant capabilities
/// is recorded for compliance and debugging purposes.
/// </summary>
[Table("capability_audit_logs")]
[Index(nameof(TenantId), nameof(ChangedAt))]
[Index(nameof(CapabilityKey))]
public class CapabilityAuditLog : EntityBase
{
    /// <summary>
    /// The tenant whose capability was changed.
    /// </summary>
    public new Guid TenantId { get; set; }

    /// <summary>
    /// The capability that was modified (e.g., "lxp.discovery", "lms.certificates").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CapabilityKey { get; set; } = string.Empty;

    /// <summary>
    /// The previous enabled state before the change.
    /// Null if this is the first time the capability was set.
    /// </summary>
    public bool? OldValue { get; set; }

    /// <summary>
    /// The new enabled state after the change.
    /// </summary>
    public bool NewValue { get; set; }

    /// <summary>
    /// The previous source of the capability (e.g., "plan:free").
    /// </summary>
    [MaxLength(100)]
    public string? OldSource { get; set; }

    /// <summary>
    /// The new source of the capability (e.g., "plan:pro", "override:admin").
    /// </summary>
    [MaxLength(100)]
    public string? NewSource { get; set; }

    /// <summary>
    /// The user who made the change.
    /// Null for system-initiated changes (e.g., subscription upgrades).
    /// </summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>
    /// The reason for the change (for compliance and support).
    /// </summary>
    [MaxLength(500)]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public CapabilityChangeType ChangeType { get; set; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>
    /// IP address of the user who made the change (for security auditing).
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client that made the change.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations.
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Type of capability change for audit categorization.
/// </summary>
public enum CapabilityChangeType
{
    /// <summary>
    /// Capability was granted for the first time.
    /// </summary>
    Granted = 0,

    /// <summary>
    /// Capability was revoked.
    /// </summary>
    Revoked = 1,

    /// <summary>
    /// Capability was modified (e.g., source changed but still enabled).
    /// </summary>
    Modified = 2,

    /// <summary>
    /// Capability expired automatically.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Capability was restored after being revoked.
    /// </summary>
    Restored = 4,

    /// <summary>
    /// Admin override was applied.
    /// </summary>
    AdminOverride = 5,

    /// <summary>
    /// Subscription plan change affected the capability.
    /// </summary>
    PlanChange = 6
}
