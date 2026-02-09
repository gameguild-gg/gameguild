using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Localization;

/// <summary>
/// Caching decorator for ILocalizationService to prevent N+1 queries and improve performance.
/// Wraps the underlying localization service with memory caching.
/// </summary>
public class CachedLocalizationService : ILocalizationService
{
    private readonly ILocalizationService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedLocalizationService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    private const string CachePrefix = "loc:";

    public CachedLocalizationService(
        ILocalizationService inner,
        IMemoryCache cache,
        ILogger<CachedLocalizationService> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResourceLocalization?> GetLocalizationAsync(
        Guid resourceId, 
        Guid languageId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(resourceId, languageId);

        if (_cache.TryGetValue(cacheKey, out ResourceLocalization? cached))
        {
            _logger.LogDebug("Cache hit for localization {ResourceId}:{LanguageId}", resourceId, languageId);
            return cached;
        }

        _logger.LogDebug("Cache miss for localization {ResourceId}:{LanguageId}", resourceId, languageId);
        var result = await _inner.GetLocalizationAsync(resourceId, languageId, cancellationToken).ConfigureAwait(false);

        if (result != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration,
                Size = 1 // For cache size limiting
            };
            _cache.Set(cacheKey, result, cacheOptions);
        }

        return result;
    }

    public async Task<IReadOnlyList<ResourceLocalization>> GetAllLocalizationsAsync(
        Guid resourceId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}all:{resourceId}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ResourceLocalization>? cached))
        {
            _logger.LogDebug("Cache hit for all localizations {ResourceId}", resourceId);
            return cached!;
        }

        _logger.LogDebug("Cache miss for all localizations {ResourceId}", resourceId);
        var result = await _inner.GetAllLocalizationsAsync(resourceId, cancellationToken).ConfigureAwait(false);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Size = result.Count
        };
        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<ResourceLocalization> CreateLocalizationAsync(
        ResourceLocalization localization, 
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.CreateLocalizationAsync(localization, cancellationToken).ConfigureAwait(false);
        InvalidateCache(localization.ResourceId, localization.LanguageId);
        return result;
    }

    public async Task<ResourceLocalization> UpdateLocalizationAsync(
        ResourceLocalization localization, 
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.UpdateLocalizationAsync(localization, cancellationToken).ConfigureAwait(false);
        InvalidateCache(localization.ResourceId, localization.LanguageId);
        return result;
    }

    public async Task DeleteLocalizationAsync(
        Guid resourceId, 
        Guid languageId, 
        CancellationToken cancellationToken = default)
    {
        await _inner.DeleteLocalizationAsync(resourceId, languageId, cancellationToken).ConfigureAwait(false);
        InvalidateCache(resourceId, languageId);
    }

    public async Task<IReadOnlyList<ResourceLocalization>> GetLocalizationsForFieldAsync(
        Guid resourceId, 
        string fieldName, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}field:{resourceId}:{fieldName}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ResourceLocalization>? cached))
        {
            return cached!;
        }

        var result = await _inner.GetLocalizationsForFieldAsync(resourceId, fieldName, cancellationToken).ConfigureAwait(false);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            Size = result.Count
        };
        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    /// <summary>
    /// Invalidates all cache entries related to a resource and language.
    /// </summary>
    private void InvalidateCache(Guid resourceId, Guid languageId)
    {
        _logger.LogDebug("Invalidating cache for {ResourceId}:{LanguageId}", resourceId, languageId);
        
        // Remove specific localization
        _cache.Remove(BuildCacheKey(resourceId, languageId));
        
        // Remove all localizations for resource
        _cache.Remove($"{CachePrefix}all:{resourceId}");
        
        // Note: Field-specific caches will expire naturally
        // A more sophisticated implementation could track and invalidate these too
    }

    private static string BuildCacheKey(Guid resourceId, Guid languageId) 
        => $"{CachePrefix}{resourceId}:{languageId}";
}
