using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flags by environment
/// </summary>
public sealed record GetFeatureFlagsByEnvironmentQuery : IQuery<IEnumerable<FeatureFlagDto>>
{
    public required string Environment { get; init; }

    public bool? IsEnabled { get; init; }
}
