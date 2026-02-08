using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flag analytics
/// </summary>
public sealed class GetFeatureFlagAnalyticsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagAnalyticsQuery, FeatureFlagAnalytics>
{
    public async Task<FeatureFlagAnalytics> Handle(GetFeatureFlagAnalyticsQuery request, CancellationToken cancellationToken)
    {
        // Get analytics data from repository
        var analytics = await repository.GetAnalyticsAsync(request.FeatureKey, request.StartDate, request.EndDate, request.Environment, request.TenantId, cancellationToken).ConfigureAwait(false);

        return analytics;
    }
}
