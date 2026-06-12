using System.ComponentModel.DataAnnotations;

namespace GameGuild.Features;

/// <summary>
///     Persistent dependency edge between two feature flags.
/// </summary>
public class FeatureFlagDependencyLink : EntityBase
{
    private FeatureFlagDependencyLink() { }

    public Guid FeatureFlagId { get; private set; }

    public Guid DependsOnFeatureFlagId { get; private set; }

    [MaxLength(50)]
    public string DependencyType { get; private set; } = string.Empty;

    public virtual FeatureFlag FeatureFlag { get; private set; } = null!;

    public virtual FeatureFlag DependsOnFeatureFlag { get; private set; } = null!;

    public static FeatureFlagDependencyLink Create(Guid featureFlagId, Guid dependsOnFeatureFlagId, string dependencyType)
    {
        if (featureFlagId == Guid.Empty)
            throw new ArgumentException("Feature flag id is required.", nameof(featureFlagId));
        if (dependsOnFeatureFlagId == Guid.Empty)
            throw new ArgumentException("Depends-on feature flag id is required.", nameof(dependsOnFeatureFlagId));
        if (featureFlagId == dependsOnFeatureFlagId)
            throw new ArgumentException("A feature flag cannot depend on itself.", nameof(dependsOnFeatureFlagId));
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyType);

        return new FeatureFlagDependencyLink
        {
            FeatureFlagId = featureFlagId,
            DependsOnFeatureFlagId = dependsOnFeatureFlagId,
            DependencyType = dependencyType.Trim(),
        };
    }
}
