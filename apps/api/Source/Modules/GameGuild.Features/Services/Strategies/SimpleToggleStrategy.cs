namespace GameGuild.Features;

/// <summary>
///     Strategy for simple boolean toggle feature flags.
/// </summary>
public class SimpleToggleStrategy : IFeatureEvaluationStrategy
{
    public FeatureFlagType FeatureType { get => FeatureFlagType.Toggle; }

    public Task<FeatureEvaluationResult> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        var result = new FeatureEvaluationResult
        {
            IsEnabled = featureFlag.IsEnabled, Value = featureFlag.IsEnabled ? featureFlag.EnabledValue : featureFlag.DefaultValue, Reason = featureFlag.IsEnabled ? "Feature is enabled" : "Feature is disabled"
        };

        return Task.FromResult(result);
    }
}
