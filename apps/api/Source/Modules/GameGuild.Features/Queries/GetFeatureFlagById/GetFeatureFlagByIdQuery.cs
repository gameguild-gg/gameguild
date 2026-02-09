using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get a feature flag by ID
/// </summary>
public sealed record GetFeatureFlagByIdQuery : IQuery<FeatureFlagDto?>
{
    public required Guid Id { get; init; }
}
