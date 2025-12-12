using GameGuild.CQRS;
using GameGuild.Features.Abstractions;
using GameGuild.Features.DTOs;

namespace GameGuild.Features.Queries.Handlers;

/// <summary>
///     Handler for retrieving feature flag evaluation history
/// </summary>
public sealed class GetFeatureFlagEvaluationHistoryQueryHandler(IFeatureFlagQueryRepository repository) : IQueryHandler<GetFeatureFlagEvaluationHistoryQuery, PagedResult<FeatureFlagEvaluationHistory>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PagedResult<FeatureFlagEvaluationHistory>> Handle(GetFeatureFlagEvaluationHistoryQuery request, CancellationToken cancellationToken)
    {
        // Get evaluation history for the feature flag
        var history = await _repository.GetEvaluationHistoryAsync(request.FeatureKey, request.StartDate, request.EndDate, request.TenantId, request.UserId, request.Page, request.PageSize, cancellationToken);

        return history;
    }
}
