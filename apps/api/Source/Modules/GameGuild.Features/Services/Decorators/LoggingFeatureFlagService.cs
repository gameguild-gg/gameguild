
using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Decorator that adds logging to feature flag evaluation.
/// </summary>
public class LoggingFeatureFlagService(IFeatureFlagEvaluationService innerService, ILogger<LoggingFeatureFlagService> logger) : IFeatureFlagEvaluationService
{
    public async Task<FeatureEvaluationResult> EvaluateAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Evaluating feature {FeatureKey} for tenant {TenantId}, user {UserId}", featureKey, context.TenantId, context.UserId);

        try
        {
            var result = await innerService.EvaluateAsync(featureKey, context, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Feature {FeatureKey} evaluated to {IsEnabled} (value: {Value}) for tenant {TenantId}", featureKey, result.IsEnabled, result.Value, context.TenantId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error evaluating feature {FeatureKey} for tenant {TenantId}", featureKey, context.TenantId);

            throw;
        }
    }

    public Task<BulkEvaluateFeaturesResponse> EvaluateBulkAsync(BulkEvaluationRequest request, CancellationToken cancellationToken = default) { return innerService.EvaluateBulkAsync(request, cancellationToken); }

    public Task<bool> IsEnabledAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default) { return innerService.IsEnabledAsync(featureKey, context, cancellationToken); }

    public Task<T> GetValueAsync<T>(string featureKey, FeatureContext context, T defaultValue, CancellationToken cancellationToken = default)
    {
        return innerService.GetValueAsync(featureKey, context, defaultValue, cancellationToken);
    }

    public Task<IEnumerable<string>> GetEnabledFeaturesAsync(FeatureContext context, CancellationToken cancellationToken = default) { return innerService.GetEnabledFeaturesAsync(context, cancellationToken); }
}
