using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
/// Represents a tenant's capability entitlement, bridging feature flags and subscription plans.
/// Each record tracks whether a specific capability is enabled for a tenant.
/// </summary>
[Table("tenant_capabilities")]
[Index(nameof(TenantId), nameof(CapabilityKey), IsUnique = true)]
public class TenantCapability : EntityBase
{
    /// <summary>
    /// The tenant this capability belongs to.
    /// </summary>
    public new Guid TenantId { get; set; }

    /// <summary>
    /// The capability key (e.g., "lxp.discovery", "lxp.learningPaths", "lms.certificates").
    /// Uses dot notation for hierarchical categorization.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CapabilityKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether this capability is currently enabled for the tenant.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The source of this capability entitlement.
    /// Examples: "plan:free", "plan:pro", "override:admin", "trial", "promotional".
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Optional expiration date for time-limited capabilities (trials, promotions).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Priority for conflict resolution when multiple sources grant/deny the same capability.
    /// Higher values take precedence. Admin overrides typically have priority 1000+.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Additional metadata about this capability (e.g., usage limits, tier-specific configs).
    /// Stored as JSON.
    /// </summary>
    [MaxLength(4000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// The user who last modified this capability override.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Reason for the last modification (for audit purposes).
    /// </summary>
    [MaxLength(500)]
    public string? ModificationReason { get; set; }
}
