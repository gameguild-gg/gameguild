using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flag statistics
/// </summary>
public sealed class GetFeatureFlagStatisticsQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagStatisticsQuery, FeatureFlagStatistics>
{
    public async Task<FeatureFlagStatistics> Handle(GetFeatureFlagStatisticsQuery request, CancellationToken cancellationToken)
    {
        // Get statistics from repository
        var statistics = await repository.GetStatisticsAsync(request.Environment, request.StartDate, request.EndDate, cancellationToken);

        return statistics;
    }
}
