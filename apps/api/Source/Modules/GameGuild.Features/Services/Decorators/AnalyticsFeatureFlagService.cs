using GameGuild.Features.Abstractions;
using GameGuild.Features.Models;

namespace GameGuild.Features.Services.Decorators;

/// <summary>
///     Decorator that adds analytics tracking to feature flag evaluation.
/// </summary>
public class AnalyticsFeatureFlagService(IFeatureFlagEvaluationService innerService, IFeatureFlagAnalyticsService analyticsService) : IFeatureFlagEvaluationService
{
    public async Task<FeatureEvaluationResult> EvaluateAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default)
    {
        var result = await innerService.EvaluateAsync(featureKey, context, cancellationToken);

        // Track the evaluation asynchronously (fire and forget)
        _ = Task.Run(
            async () =>
            {
                try { await analyticsService.RecordUsageAsync(featureKey, context, result.IsEnabled, result.Value, cancellationToken); }
                catch
                {
                    // Silently fail - analytics should not affect feature evaluation
                }
            },
            cancellationToken
        );

        return result;
    }

    public Task<BulkEvaluateFeaturesResponse> EvaluateBulkAsync(BulkEvaluationRequest request, CancellationToken cancellationToken = default) { return innerService.EvaluateBulkAsync(request, cancellationToken); }

    public Task<bool> IsEnabledAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default) { return innerService.IsEnabledAsync(featureKey, context, cancellationToken); }

    public Task<T> GetValueAsync<T>(string featureKey, FeatureContext context, T defaultValue, CancellationToken cancellationToken = default)
    {
        return innerService.GetValueAsync(featureKey, context, defaultValue, cancellationToken);
    }

    public Task<IEnumerable<string>> GetEnabledFeaturesAsync(FeatureContext context, CancellationToken cancellationToken = default) { return innerService.GetEnabledFeaturesAsync(context, cancellationToken); }
}
