
namespace GameGuild.Features;

/// <summary>
///     Strategy for percentage-based rollout feature flags.
/// </summary>
public class PercentageRolloutStrategy : IFeatureEvaluationStrategy
{
    public FeatureFlagType FeatureType { get => FeatureFlagType.Percentage; }

    public Task<FeatureEvaluationResult> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (!featureFlag.IsEnabled) { return Task.FromResult(new FeatureEvaluationResult { IsEnabled = false, Value = featureFlag.DefaultValue, Reason = "Feature is disabled" }); }

        var rolloutPercentage = featureFlag.RolloutPercentage;

        var identifier = RolloutHashCalculator.CreateIdentifier(context);
        var isInRollout = RolloutHashCalculator.IsInRollout(identifier, rolloutPercentage, featureFlag.Key);

        var result = new FeatureEvaluationResult
        {
            IsEnabled = isInRollout,
            Value = isInRollout ? featureFlag.EnabledValue : featureFlag.DefaultValue,
            Reason = isInRollout ? $"User is in {rolloutPercentage}% rollout" : $"User is not in {rolloutPercentage}% rollout"
        };

        return Task.FromResult(result);
    }
}
