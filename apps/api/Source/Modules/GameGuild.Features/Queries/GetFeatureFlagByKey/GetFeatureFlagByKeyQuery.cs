using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get a feature flag by key
/// </summary>
public sealed record GetFeatureFlagByKeyQuery : IQuery<FeatureFlagDto?>
{
    public required string Key { get; init; }
}
