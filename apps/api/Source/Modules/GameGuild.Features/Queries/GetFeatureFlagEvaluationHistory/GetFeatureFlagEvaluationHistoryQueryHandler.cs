
namespace GameGuild.Features;

/// <summary>
///     Handler for retrieving feature flag evaluation history
/// </summary>
public sealed class GetFeatureFlagEvaluationHistoryQueryHandler(IFeatureFlagQueryRepository repository) : CQRS.IQueryHandler<GetFeatureFlagEvaluationHistoryQuery, PagedResult<FeatureFlagEvaluationHistory>>
{
    private readonly IFeatureFlagQueryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PagedResult<FeatureFlagEvaluationHistory>> Handle(GetFeatureFlagEvaluationHistoryQuery request, CancellationToken cancellationToken)
    {
        // Get evaluation history for the feature flag
        var history = await _repository.GetEvaluationHistoryAsync(request.FeatureKey, request.StartDate, request.EndDate, request.TenantId, request.UserId, request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        return history;
    }
}
