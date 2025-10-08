using MediatR;
using GameGuild.Modules.Features.DTOs;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get all feature flags
/// </summary>
public sealed record GetAllFeatureFlagsQuery : IRequest<IEnumerable<FeatureFlagDto>>
{
    public string? Environment { get; init; }

    public bool? IsEnabled { get; init; }

    public bool? IsGlobal { get; init; }

    public int? Skip { get; init; }

    public int? Take { get; init; }
}

