using GameGuild.CQRS;
using GameGuild.Modules.Features.DTOs;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get a feature flag by ID
/// </summary>
public sealed record GetFeatureFlagByIdQuery : IRequest<FeatureFlagDto?>
{
    public required Guid Id { get; init; }
}

