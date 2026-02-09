using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to evaluate multiple feature flags in bulk
/// </summary>
public sealed record BulkEvaluateFeaturesQuery : IQuery<BulkEvaluateFeaturesResponse>
{
    /// <summary>
    ///     Feature flag keys to evaluate
    /// </summary>
    public required IEnumerable<string> FeatureKeys { get; init; }

    /// <summary>
    ///     Evaluation context for all features
    /// </summary>
    public required FeatureContext Context { get; init; }
}
