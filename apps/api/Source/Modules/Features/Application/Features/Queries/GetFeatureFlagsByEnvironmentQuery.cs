using MediatR;
using GameGuild.Modules.Features.DTOs;
namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flags by environment
/// </summary>
public sealed record GetFeatureFlagsByEnvironmentQuery : IRequest<IEnumerable<FeatureFlagDto>>
{
    public required string Environment { get; init; }

    public bool? IsEnabled { get; init; }
}

