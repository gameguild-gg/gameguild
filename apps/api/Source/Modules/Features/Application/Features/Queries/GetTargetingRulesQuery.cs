using MediatR;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get targeting rules for a feature flag
/// </summary>
public sealed record GetTargetingRulesQuery : IRequest<IEnumerable<FeatureFlagTargetDto>>
{
    public required Guid FeatureFlagId { get; init; }
}

