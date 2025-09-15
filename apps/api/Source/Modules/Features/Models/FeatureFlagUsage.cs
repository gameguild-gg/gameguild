using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Modules.Features.Models;

/// <summary>
/// Represents usage analytics for feature flags
/// </summary>
[Table("FeatureFlagUsage")]
public class FeatureFlagUsage : EntityBase
{
    /// <summary>
    /// Feature flag this usage record belongs to
    /// </summary>
    [Required]
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    /// User who triggered the feature flag evaluation
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tenant context for the evaluation
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Whether the feature was enabled for this evaluation
    /// </summary>
    public bool WasEnabled { get; set; }

    /// <summary>
    /// Value returned by the feature flag
    /// </summary>
    [MaxLength(1000)]
    public string? ReturnedValue { get; set; }

    /// <summary>
    /// Environment where the evaluation happened
    /// </summary>
    [MaxLength(50)]
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the evaluation result
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Additional context data (JSON)
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Navigation property to feature flag
    /// </summary>
    public virtual FeatureFlag FeatureFlag { get; set; } = null!;
}
