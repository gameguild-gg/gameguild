using GameGuild.CQRS;
using GameGuild.Modules.Features.DTOs;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get a feature flag by key
/// </summary>
public sealed record GetFeatureFlagByKeyQuery : IRequest<FeatureFlagDto?>
{
    public required string Key { get; init; }
}

