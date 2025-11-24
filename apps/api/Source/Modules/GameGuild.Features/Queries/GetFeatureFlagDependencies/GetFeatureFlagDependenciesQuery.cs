using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get feature flag dependencies
/// </summary>
public record GetFeatureFlagDependenciesQuery : IQuery<IEnumerable<FeatureFlagDependency>>
{
    public required Guid FeatureFlagId { get; init; }

    public bool IncludeInverse { get; init; }
}
