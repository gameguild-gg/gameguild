using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get targeting rule by ID
/// </summary>
public sealed record GetTargetingRuleByIdQuery : IQuery<FeatureFlagTargetDto?>
{
    public required Guid Id { get; init; }
}
