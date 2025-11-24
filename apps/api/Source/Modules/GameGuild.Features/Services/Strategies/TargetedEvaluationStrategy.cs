using GameGuild.Features.Abstractions;
using GameGuild.Features.Entities;
using GameGuild.Features.Models;
using FeatureFlagType = GameGuild.Features.Entities.FeatureFlagType;

namespace GameGuild.Features.Services.Strategies;

/// <summary>
///     Strategy for targeted feature flags with rule-based evaluation.
/// </summary>
public class TargetedEvaluationStrategy(IEnumerable<ITargetingRuleHandler> handlers) : IFeatureEvaluationStrategy
{
    public FeatureFlagType FeatureType { get => FeatureFlagType.UserSegment; }

    public async Task<FeatureEvaluationResult> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (!featureFlag.IsEnabled) { return new FeatureEvaluationResult { IsEnabled = false, Value = featureFlag.DefaultValue, Reason = "Feature is disabled" }; }

        if (featureFlag.Targets == null || !featureFlag.Targets.Any())
        {
            return new FeatureEvaluationResult { IsEnabled = true, Value = featureFlag.EnabledValue, Reason = "No targeting rules defined, feature is enabled for all" };
        }

        // Execute chain of responsibility pattern
        foreach (var handler in handlers.OrderBy(h => h.Priority))
        {
            var result = await handler.EvaluateAsync(featureFlag, context, cancellationToken);

            if (result != null) { return result; }
        }

        // No handler matched - default to disabled
        return new FeatureEvaluationResult { IsEnabled = false, Value = featureFlag.DefaultValue, Reason = "No targeting rules matched" };
    }
}
