using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flag analytics
/// </summary>
public sealed class GetFeatureFlagAnalyticsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagAnalyticsQuery, FeatureFlagAnalytics>
{
    public async Task<FeatureFlagAnalytics> Handle(GetFeatureFlagAnalyticsQuery request, CancellationToken cancellationToken)
    {
        // Get analytics data from repository
        var analytics = await repository.GetAnalyticsAsync(request.FeatureKey, request.StartDate, request.EndDate, request.Environment, request.TenantId, cancellationToken);

        return analytics;
    }
}
