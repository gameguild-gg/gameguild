using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flag statistics
/// </summary>
public sealed record GetFeatureFlagStatisticsQuery : IQuery<FeatureFlagStatistics>
{
    public string? Environment { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }
}
