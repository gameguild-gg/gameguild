using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flag usage summary
/// </summary>
public sealed record GetFeatureFlagUsageSummaryQuery : IQuery<IEnumerable<FeatureFlagUsageSummary>>
{
    public required string FeatureKey { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public string GroupBy { get; init; } = "day"; // day, hour, tenant, user
}
