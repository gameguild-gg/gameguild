using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get targeting rules for a feature flag
/// </summary>
public sealed record GetTargetingRulesQuery : IQuery<IEnumerable<FeatureFlagTargetDto>>
{
    public required Guid FeatureFlagId { get; init; }
}
