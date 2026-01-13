using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing an audit trail entry</summary>
[Table("audit_trails")]
[Index(nameof(EntityType))]
[Index(nameof(EntityId))]
[Index(nameof(Action))]
[Index(nameof(ChangedBy))]
[Index(nameof(ChangedAt))]
public class AuditTrail : EntityBase
{
    /// <summary>Default constructor</summary>
    public AuditTrail() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial audit data</param>
    public AuditTrail(object partial) : base(partial) { }

    /// <summary>Entity type</summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Entity ID</summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>Audit action</summary>
    public AuditAction Action { get; set; }

    /// <summary>Old value (JSON)</summary>
    [MaxLength(4000)]
    public string? OldValue { get; set; }

    /// <summary>New value (JSON)</summary>
    [MaxLength(4000)]
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

    /// <summary>Metadata (JSON)</summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>Change reason</summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
}
