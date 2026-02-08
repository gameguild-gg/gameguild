using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flag usage summary
/// </summary>
public sealed class GetFeatureFlagUsageSummaryQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagUsageSummaryQuery, IEnumerable<FeatureFlagUsageSummary>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagUsageSummary>> Handle(GetFeatureFlagUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        // Get usage summary for the feature flag
        var summary = await _repository.GetUsageSummaryAsync(request.FeatureKey, request.StartDate, request.EndDate, request.GroupBy, cancellationToken).ConfigureAwait(false);

        return summary;
    }
}
