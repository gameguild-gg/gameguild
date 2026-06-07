using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Features;

/// <summary>
///     Represents a feature flag in the system
/// </summary>
[Table("feature_flags")]
[Index(nameof(Key), IsUnique = true)]
public class FeatureFlag : EntityBase
{
    /// <summary>
    ///     Unique key for the feature flag
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the feature flag
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description of what this feature flag controls
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this feature flag is currently enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Type of the feature flag
    /// </summary>
    public FeatureFlagType Type { get; set; } = FeatureFlagType.Toggle;

    /// <summary>
    ///     Default value when the feature flag is disabled or not found
    /// </summary>
    [MaxLength(1000)]
    public string? DefaultValue { get; set; }

    /// <summary>
    ///     Value when the feature flag is enabled
    /// </summary>
    [MaxLength(1000)]
    public string? EnabledValue { get; set; }

    /// <summary>
    ///     Whether this feature flag applies to all tenants (global)
    /// </summary>
    public new bool IsGlobal { get; set; }

    /// <summary>
    ///     Percentage rollout (0-100) for gradual feature releases
    /// </summary>
    [Range(0, 100)]
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    ///     Environment where this feature flag is active (e.g., "development", "staging", "production")
    /// </summary>
    [MaxLength(50)]
    public string Environment { get; set; } = "production";

    /// <summary>
    ///     Date and time when this feature flag expires (for temporary flags)
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    ///     Date when this feature flag should be reviewed for removal or update
    /// </summary>
    public DateTimeOffset? ReviewDate { get; set; }

    /// <summary>
    ///     Whether this is a kill switch flag for emergency global shutoff
    /// </summary>
    public bool IsKillSwitch { get; set; }

    /// <summary>
    ///     Owner or team responsible for this feature flag
    /// </summary>
    [MaxLength(200)]
    public string? Owner { get; set; }

    /// <summary>
    ///     Contact information for escalation regarding this flag
    /// </summary>
    [MaxLength(500)]
    public string? EscalationContact { get; set; }

    /// <summary>
    ///     Additional governance notes and metadata
    /// </summary>
    [MaxLength(2000)]
    public string? GovernanceNotes { get; set; }

    /// <summary>
    ///     Whether this flag's sensitive values should be encrypted at rest
    /// </summary>
    public bool RequiresEncryption { get; set; }

    /// <summary>
    ///     Navigation property to feature flag targets
    /// </summary>
    public virtual ICollection<FeatureFlagTarget> Targets { get; init; } = [];

    /// <summary>
    ///     Navigation property to feature flag usage analytics
    /// </summary>
    public virtual ICollection<FeatureFlagUsage> UsageAnalytics { get; init; } = [];

    /// <summary>
    ///     Checks if the feature flag has expired based on ExpiresAt
    /// </summary>
    public bool IsExpired() { return ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow; }

    /// <summary>
    ///     Checks if the feature flag is stale and needs review
    /// </summary>
    public bool IsStale() { return ReviewDate.HasValue && ReviewDate.Value < DateTimeOffset.UtcNow; }

    /// <summary>
    ///     Gets the days until expiration (null if no expiry set)
    /// </summary>
    public int? GetDaysUntilExpiration()
    {
        if (!ExpiresAt.HasValue) return null;

        var days = (ExpiresAt.Value - DateTimeOffset.UtcNow).Days;

        return days < 0 ? 0 : days;
    }

    /// <summary>
    ///     Gets the days until review is due (null if no review date set)
    /// </summary>
    public int? GetDaysUntilReview()
    {
        if (!ReviewDate.HasValue) return null;

        var days = (ReviewDate.Value - DateTimeOffset.UtcNow).Days;

        return days < 0 ? 0 : days;
    }
}
