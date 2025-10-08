using GameGuild.Modules.Features.Entities;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Abstractions;

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

