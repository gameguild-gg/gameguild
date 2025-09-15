namespace GameGuild.Modules.Features.Models;

/// <summary>
/// Represents targeting rules for feature flags
/// </summary>
[Table("FeatureFlagTargets")]
public class FeatureFlagTarget : EntityBase
{
    /// <summary>
    /// Feature flag this target belongs to
    /// </summary>
    [Required]
    public Guid FeatureFlagId { get; set; }

    /// <summary>
    /// Type of target (user, tenant, role, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Target identifier (user ID, tenant ID, role name, etc.)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string TargetIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Whether this target is included or excluded
    /// </summary>
    public bool IsIncluded { get; set; } = true;

    /// <summary>
    /// Custom value for this target (overrides default)
    /// </summary>
    [MaxLength(1000)]
    public string? Value { get; set; }

    /// <summary>
    /// Navigation property to feature flag
    /// </summary>
    public virtual FeatureFlag FeatureFlag { get; set; } = null!;
}
