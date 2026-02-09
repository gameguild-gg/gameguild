using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flag dependencies
/// </summary>
public sealed record GetFeatureFlagDependenciesQuery : IQuery<IEnumerable<FeatureFlagDependency>>
{
    public required Guid FeatureFlagId { get; init; }

    public bool IncludeInverse { get; init; }
}
