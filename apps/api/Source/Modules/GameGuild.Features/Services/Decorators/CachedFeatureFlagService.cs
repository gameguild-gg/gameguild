using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;

namespace GameGuild.Features;

/// <summary>
///     Decorator that adds caching to feature flag evaluation.
/// </summary>
public class CachedFeatureFlagService(IFeatureFlagEvaluationService innerService, IDistributedCache cache) : IFeatureFlagEvaluationService
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public async Task<FeatureEvaluationResult> EvaluateAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(featureKey, context);

        var cachedValue = await cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);

        if (cachedValue != null) { return JsonSerializer.Deserialize<FeatureEvaluationResult>(cachedValue)!; }

        var result = await innerService.EvaluateAsync(featureKey, context, cancellationToken).ConfigureAwait(false);

        var serialized = JsonSerializer.Serialize(result);
        await cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheDuration }, cancellationToken).ConfigureAwait(false);

        return result;
    }

    public Task<BulkEvaluateFeaturesResponse> EvaluateBulkAsync(BulkEvaluationRequest request, CancellationToken cancellationToken = default) { return innerService.EvaluateBulkAsync(request, cancellationToken); }

    public Task<bool> IsEnabledAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default) { return innerService.IsEnabledAsync(featureKey, context, cancellationToken); }

    public Task<T> GetValueAsync<T>(string featureKey, FeatureContext context, T defaultValue, CancellationToken cancellationToken = default)
    {
        return innerService.GetValueAsync(featureKey, context, defaultValue, cancellationToken);
    }

    public Task<IEnumerable<string>> GetEnabledFeaturesAsync(FeatureContext context, CancellationToken cancellationToken = default) { return innerService.GetEnabledFeaturesAsync(context, cancellationToken); }

    private string GenerateCacheKey(string featureKey, FeatureContext context) { return $"feature:{featureKey}:tenant:{context.TenantId}:user:{context.UserId}"; }
}
