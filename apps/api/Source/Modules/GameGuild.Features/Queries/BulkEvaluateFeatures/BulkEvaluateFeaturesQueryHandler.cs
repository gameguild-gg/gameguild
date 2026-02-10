using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for bulk feature flag evaluation
/// </summary>
public sealed class BulkEvaluateFeaturesQueryHandler(IFeatureFlagEvaluationService evaluationService) : IQueryHandler<BulkEvaluateFeaturesQuery, BulkEvaluateFeaturesResponse>
{
    private readonly IFeatureFlagEvaluationService _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));

    public async Task<BulkEvaluateFeaturesResponse> Handle(BulkEvaluateFeaturesQuery request, CancellationToken cancellationToken)
    {
        // Evaluate all feature flags in parallel
        var evaluationTasks = request.FeatureKeys.Select(async featureKey =>
            {
                var result = await _evaluationService.EvaluateAsync(featureKey, request.Context, cancellationToken).ConfigureAwait(false);

                return new KeyValuePair<string, FeatureEvaluationResult>(featureKey, result);
            }
        );

        var results = await Task.WhenAll(evaluationTasks).ConfigureAwait(false);

        return new BulkEvaluateFeaturesResponse { Results = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), EvaluatedAt = SystemClock.UtcNow };
    }
}
