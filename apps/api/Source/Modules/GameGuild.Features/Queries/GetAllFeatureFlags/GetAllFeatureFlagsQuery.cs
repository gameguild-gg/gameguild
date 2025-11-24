using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get all feature flags
/// </summary>
public sealed record GetAllFeatureFlagsQuery : IQuery<IEnumerable<FeatureFlagDto>>
{
    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }

    public bool? IsGlobal { get; init; }

    public int? Skip { get; init; }

    public int? Take { get; init; }
}
