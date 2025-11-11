using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get targeting rules for a feature flag
/// </summary>
public record GetTargetingRulesQuery : IQuery<IEnumerable<FeatureFlagTargetDto>>
{
    public required Guid FeatureFlagId { get; init; }
}
