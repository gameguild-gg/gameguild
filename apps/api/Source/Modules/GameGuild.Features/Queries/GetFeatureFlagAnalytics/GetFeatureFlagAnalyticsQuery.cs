using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Query to get feature flag usage analytics
/// </summary>
public sealed record GetFeatureFlagAnalyticsQuery : IQuery<FeatureFlagAnalytics>
{
    public required string FeatureKey { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public string? Environment { get; init; }

    public Guid? TenantId { get; init; }
}
