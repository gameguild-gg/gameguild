using GameGuild.CQRS;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to evaluate multiple feature flags in bulk
/// </summary>
public record BulkEvaluateFeaturesQuery : IQuery<BulkEvaluateFeaturesResponse>
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
