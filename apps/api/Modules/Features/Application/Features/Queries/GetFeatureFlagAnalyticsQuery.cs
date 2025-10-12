using GameGuild.CQRS;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Queries;

/// <summary>
///     Query to get feature flag usage analytics
/// </summary>
public sealed record GetFeatureFlagAnalyticsQuery : IRequest<FeatureFlagAnalytics>
{
    public required string FeatureKey { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public string? Environment { get; init; }

    public Guid? TenantId { get; init; }
}

