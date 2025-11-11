using GameGuild.Features.Entities;
using GameGuild.Features.Models;
using FeatureFlagType = GameGuild.Features.Entities.FeatureFlagType;

namespace GameGuild.Features.Abstractions;

/// <summary>
///     Strategy interface for different feature evaluation algorithms.
/// </summary>
public interface IFeatureEvaluationStrategy
{
    /// <summary>
    ///     The feature type this strategy handles.
    /// </summary>
    FeatureFlagType FeatureType { get; }

    /// <summary>
    ///     Evaluates a feature flag using this strategy.
    /// </summary>
    Task<FeatureEvaluationResult> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default);
}
