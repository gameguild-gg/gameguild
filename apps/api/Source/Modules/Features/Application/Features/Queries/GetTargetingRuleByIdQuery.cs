using GameGuild.CQRS;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get targeting rule by ID
/// </summary>
public sealed record GetTargetingRuleByIdQuery : IRequest<FeatureFlagTargetDto?>
{
    public required Guid Id { get; init; }
}

