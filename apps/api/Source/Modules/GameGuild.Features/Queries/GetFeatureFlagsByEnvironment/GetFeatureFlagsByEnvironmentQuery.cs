using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get feature flags by environment
/// </summary>
public record GetFeatureFlagsByEnvironmentQuery : IQuery<IEnumerable<FeatureFlagDto>>
{
    public required string Environment { get; init; }

    public bool? IsEnabled { get; init; }
}
