using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to check if feature flag exists
/// </summary>
public sealed record FeatureFlagExistsQuery : IQuery<bool>
{
    public required string Key { get; init; }

    public string? Environment { get; init; }
}
