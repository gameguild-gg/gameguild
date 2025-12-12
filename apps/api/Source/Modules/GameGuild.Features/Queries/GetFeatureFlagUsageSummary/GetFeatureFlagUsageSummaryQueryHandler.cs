using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flag usage summary
/// </summary>
public sealed class GetFeatureFlagUsageSummaryQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagUsageSummaryQuery, IEnumerable<FeatureFlagUsageSummary>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<FeatureFlagUsageSummary>> Handle(GetFeatureFlagUsageSummaryQuery request, CancellationToken cancellationToken)
    {
        // Get usage summary for the feature flag
        var summary = await _repository.GetUsageSummaryAsync(request.FeatureKey, request.StartDate, request.EndDate, request.GroupBy, cancellationToken);

        return summary;
    }
}
