using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get a feature flag by key
/// </summary>
public sealed record GetFeatureFlagByKeyQuery : IQuery<FeatureFlagDto?>
{
    public required string Key { get; init; }
}
