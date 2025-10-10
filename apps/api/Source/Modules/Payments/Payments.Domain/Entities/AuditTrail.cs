using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Entity representing an audit trail entry</summary>
[Table("audit_trails")]
[Index(nameof(EntityType))]
[Index(nameof(EntityId))]
[Index(nameof(Action))]
[Index(nameof(ChangedBy))]
[Index(nameof(ChangedAt))]
public class AuditTrail : EntityBase
{
    /// <summary>Entity type being audited</summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity ID being audited</summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>Action performed</summary>
    public AuditAction Action { get; set; }

    /// <summary>Old value (JSON)</summary>
    [MaxLength(10000)]
    public string? OldValue { get; set; }

    /// <summary>New value (JSON)</summary>
    [MaxLength(10000)]
    public string? NewValue { get; set; }

    /// <summary>Changed by user ID</summary>
    [Required]
    public Guid ChangedBy { get; set; }

    /// <summary>Changed timestamp</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>IP address</summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>User agent</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>Additional metadata (JSON)</summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>Change reason</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>Tenant ID for multi-tenancy</summary>
    public Guid? TenantId { get; set; }
}

/// <summary>Audit actions</summary>
public enum AuditAction
{
    /// <summary>Entity created</summary>
    Created = 0,

    /// <summary>Entity updated</summary>
    Updated = 1,

    /// <summary>Entity deleted</summary>
    Deleted = 2,

    /// <summary>Entity restored</summary>
    Restored = 3,

    /// <summary>Status changed</summary>
    StatusChanged = 4,

    /// <summary>Permission changed</summary>
    PermissionChanged = 5,

    /// <summary>Configuration changed</summary>
    ConfigurationChanged = 6,

    /// <summary>Other action</summary>
    Other = 7
}
