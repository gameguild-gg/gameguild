using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get targeting rule by ID
/// </summary>
public record GetTargetingRuleByIdQuery : IQuery<FeatureFlagTargetDto?>
{
    public required Guid Id { get; init; }
}
