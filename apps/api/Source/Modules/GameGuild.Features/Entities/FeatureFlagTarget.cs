using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Features;

/// <summary>
///     Represents a feature flag targeting rule for specific tenants, users, or plans
/// </summary>
[Table("FeatureFlagTargets")]
public sealed class FeatureFlagTarget : EntityBase
{
    /// <summary>
    ///     Feature flag this target belongs to
    /// </summary>
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    ///     Type of target (tenant, user, plan, environment, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    ///     Identifier of the target (tenant ID, user ID, plan ID, etc.)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string TargetIdentifier { get; set; } = string.Empty;

    /// <summary>
    ///     Whether this target is enabled or disabled for the feature
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    ///     Percentage rollout for this specific target (0-100)
    /// </summary>
    [Range(0, 100)]
    public int RolloutPercentage { get; set; } = 100;

    /// <summary>
    ///     Custom value for this target (overrides default feature flag value)
    /// </summary>
    [MaxLength(1000)]
    public string? CustomValue { get; set; }

    /// <summary>
    ///     Additional metadata for targeting rules (JSON)
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Priority of this target rule (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    ///     Key of another feature flag this target depends on
    /// </summary>
    [MaxLength(255)]
    public string? DependsOn { get; set; }

    /// <summary>
    ///     Navigation property to the feature flag
    /// </summary>
    public FeatureFlag? FeatureFlag { get; set; }
}
