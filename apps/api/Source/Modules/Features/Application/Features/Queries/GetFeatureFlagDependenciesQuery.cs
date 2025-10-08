using MediatR;
using GameGuild.Modules.Features.DTOs;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flag dependencies
/// </summary>
public sealed record GetFeatureFlagDependenciesQuery : IRequest<IEnumerable<FeatureFlagDependency>>
{
    public required Guid FeatureFlagId { get; init; }

    public bool IncludeInverse { get; init; }
}

