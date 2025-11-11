using GameGuild.CQRS;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries;

/// <summary>
///     Query to get a feature flag by ID
/// </summary>
public record GetFeatureFlagByIdQuery : IQuery<FeatureFlagDto?>
{
    public required Guid Id { get; init; }
}
