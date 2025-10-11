using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;


namespace GameGuild.Modules.Common.Secrets;

/// <summary>
/// Secret service implementation with in-memory caching and TTL support.
/// </summary>
public sealed class SecretService : ISecretService
{
    private readonly ISecretProvider _provider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretService> _logger;
    private readonly TimeSpan _cacheDuration;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks;

    public SecretService(
        ISecretProvider provider,
        IMemoryCache cache,
        ILogger<SecretService> logger,
        TimeSpan? cacheDuration = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
        _locks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public async Task<string?> GetSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretWithMetadataAsync(key, version, cancellationToken);
        return secret?.Value;
    }

    public async Task<Secret?> GetSecretWithMetadataAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        var cacheKey = GetCacheKey(key, version);

        // Try cache first
        if (_cache.TryGetValue<Secret>(cacheKey, out var cachedSecret))
        {
            _logger.LogDebug("Secret '{Key}' retrieved from cache", key);
            return cachedSecret;
        }

        // Use lock to prevent cache stampede
        var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue<Secret>(cacheKey, out cachedSecret))
            {
                return cachedSecret;
            }

            // Fetch from provider
            _logger.LogInformation("Fetching secret '{Key}' from provider '{Provider}'", key, _provider.ProviderName);
            var secret = await _provider.GetSecretAsync(key, version, cancellationToken);

            if (secret != null)
            {
                // Cache the secret with TTL
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheDuration,
                    Size = 1 // For cache size management
                };

                _cache.Set(cacheKey, secret, cacheOptions);
                _logger.LogDebug("Secret '{Key}' cached for {Duration}", key, _cacheDuration);
            }

            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret '{Key}' from provider '{Provider}'", key, _provider.ProviderName);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task SetSecretAsync(
        string key,
        string value,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            _logger.LogInformation("Setting secret '{Key}' in provider '{Provider}'", key, _provider.ProviderName);
            var secret = await _provider.SetSecretAsync(key, value, metadata, cancellationToken);

            // Invalidate cache and update with new value
            InvalidateCache(key);
            var cacheKey = GetCacheKey(key, secret.Version);
            _cache.Set(cacheKey, secret, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheDuration,
                Size = 1
            });

            _logger.LogInformation("Secret '{Key}' set successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secret '{Key}' in provider '{Provider}'", key, _provider.ProviderName);
            throw;
        }
    }

    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            _logger.LogInformation("Deleting secret '{Key}' from provider '{Provider}'", key, _provider.ProviderName);
            await _provider.DeleteSecretAsync(key, cancellationToken);

            // Invalidate cache
            InvalidateCache(key);

            _logger.LogInformation("Secret '{Key}' deleted successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret '{Key}' from provider '{Provider}'", key, _provider.ProviderName);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing secrets from provider '{Provider}'", _provider.ProviderName);
            return await _provider.ListSecretsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list secrets from provider '{Provider}'", _provider.ProviderName);
            throw;
        }
    }

    public void InvalidateCache(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        // Remove all versions of this key from cache
        var cacheKey = GetCacheKey(key, null);
        _cache.Remove(cacheKey);

        _logger.LogDebug("Cache invalidated for secret '{Key}'", key);
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Remove 100% of cache entries
            _logger.LogInformation("All secret cache cleared");
        }
    }

    public async Task RotateSecretAsync(string key, Func<string> valueGenerator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(valueGenerator);

        try
        {
            _logger.LogInformation("Rotating secret '{Key}'", key);

            // Generate new value
            var newValue = valueGenerator();

            // Get current metadata if it exists
            var currentSecret = await GetSecretWithMetadataAsync(key, null, cancellationToken);
            var metadata = currentSecret?.Metadata;

            // Update secret
            await SetSecretAsync(key, newValue, metadata, cancellationToken);

            _logger.LogInformation("Secret '{Key}' rotated successfully", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate secret '{Key}'", key);
            throw;
        }
    }

    private static string GetCacheKey(string key, string? version)
    {
        return version != null ? $"secret:{key}:{version}" : $"secret:{key}";
    }
}
