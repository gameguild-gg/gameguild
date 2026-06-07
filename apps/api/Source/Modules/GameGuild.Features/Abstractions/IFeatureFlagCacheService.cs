namespace GameGuild.Features;

/// <summary>
///     Feature flag caching service interface
/// </summary>
public interface IFeatureFlagCacheService
{
    Task<FeatureFlagConfig?> GetCachedConfigAsync(string featureKey, CancellationToken cancellationToken = default);

    Task SetCachedConfigAsync(string featureKey, FeatureFlagConfig config, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    Task InvalidateCacheAsync(string featureKey, CancellationToken cancellationToken = default);

    Task InvalidateAllCacheAsync(CancellationToken cancellationToken = default);
}
