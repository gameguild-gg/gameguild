namespace GameGuild.Features;

/// <summary>
///     Feature flag evaluation engine interface
/// </summary>
public interface IFeatureFlagEvaluationEngine
{
    FeatureEvaluationResult EvaluateFeature(FeatureFlag featureFlag, FeatureContext context);

    bool MatchesTargetingRule(FeatureFlagTarget target, FeatureContext context);

    bool IsInRollout(FeatureContext context, int percentage, string? salt = null);

    string? EvaluateValue(FeatureFlag featureFlag, FeatureContext context, bool isEnabled);
}
