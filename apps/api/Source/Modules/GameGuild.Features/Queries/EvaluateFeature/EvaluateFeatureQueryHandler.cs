using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Handler for evaluating a feature flag
/// </summary>
public sealed class EvaluateFeatureQueryHandler(IFeatureFlagEvaluationService evaluationService) : IQueryHandler<EvaluateFeatureQuery, FeatureEvaluationResult>
{
    private readonly IFeatureFlagEvaluationService _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));

    public async Task<FeatureEvaluationResult> Handle(EvaluateFeatureQuery request, CancellationToken cancellationToken)
    {
        // Build evaluation context from query
        var context = new FeatureContext
        {
            UserId = request.UserId,
            TenantId = request.TenantId,
            Environment = request.Environment ?? "production",
            Permissions = request.Permissions ?? [],
            CustomAttributes = request.CustomAttributes ?? new Dictionary<string, object>()
        };

        // Evaluate the feature flag
        var result = await _evaluationService.EvaluateAsync(request.FeatureKey, context, cancellationToken).ConfigureAwait(false);

        return result;
    }
}
