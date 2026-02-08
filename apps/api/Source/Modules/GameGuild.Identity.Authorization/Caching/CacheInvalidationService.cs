using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.Caching;

/// <summary>
///     Unified cache invalidation service for coordinating cache coherence across services.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    ///     Invalidates all permission caches for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates permission caches for a specific user in a tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates ACL caches for a specific resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateResourceAsync(Guid tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates policy caches for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="policyName">Optional specific policy name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePolicyAsync(Guid tenantId, string? policyName = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes an invalidation event for distributed cache coherence.
    /// </summary>
    /// <param name="invalidationEvent">The invalidation event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishInvalidationAsync(CacheInvalidationEvent invalidationEvent, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Handles an invalidation event received from another instance.
    /// </summary>
    /// <param name="invalidationEvent">The invalidation event.</param>
    void HandleInvalidationEvent(CacheInvalidationEvent invalidationEvent);
}

/// <summary>
///     Event representing a cache invalidation request.
/// </summary>
public sealed class CacheInvalidationEvent
{
    /// <summary>
    ///     The type of invalidation.
    /// </summary>
    public CacheInvalidationType Type { get; set; }

    /// <summary>
    ///     The tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Optional user ID (for user-specific invalidation).
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Optional resource type (for resource-specific invalidation).
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Optional resource ID (for resource-specific invalidation).
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    ///     Optional policy name (for policy-specific invalidation).
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>
    ///     Timestamp of the invalidation event.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Instance ID that originated the event (to avoid self-handling).
    /// </summary>
    public string OriginInstanceId { get; set; } = string.Empty;
}

/// <summary>
///     Types of cache invalidation.
/// </summary>
public enum CacheInvalidationType
{
    /// <summary>
    ///     Invalidate all caches for a tenant.
    /// </summary>
    Tenant,

    /// <summary>
    ///     Invalidate caches for a specific user.
    /// </summary>
    User,

    /// <summary>
    ///     Invalidate caches for a specific resource.
    /// </summary>
    Resource,

    /// <summary>
    ///     Invalidate policy caches.
    /// </summary>
    Policy
}

/// <summary>
///     Default implementation of <see cref="ICacheInvalidationService"/>.
/// </summary>
public sealed class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITenantSecurityVersionStore _versionStore;
    private readonly IHybridPermissionCache _hybridCache;
    private readonly ICacheMetricsService _metrics;
    private readonly AuthorizationCacheOptions _options;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly string _instanceId;

    // Tracks cache keys by tenant for efficient invalidation
    private readonly Dictionary<Guid, HashSet<string>> _tenantCacheKeys = new();
    private readonly object _keysLock = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="CacheInvalidationService"/>.
    /// </summary>
    public CacheInvalidationService(
        IMemoryCache memoryCache,
        ITenantSecurityVersionStore versionStore,
        IHybridPermissionCache hybridCache,
        ICacheMetricsService metrics,
        IOptions<AuthorizationCacheOptions> options,
        ILogger<CacheInvalidationService> logger)
    {
        _memoryCache = memoryCache;
        _versionStore = versionStore;
        _hybridCache = hybridCache;
        _metrics = metrics;
        _options = options.Value;
        _logger = logger;
        _instanceId = Guid.NewGuid().ToString("N")[..8];
    }

    /// <inheritdoc />
    public async Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating all caches for tenant {TenantId}", tenantId);

        // Increment version to invalidate version-keyed caches
        await _versionStore.IncrementVersionAsync(tenantId.ToString(), cancellationToken).ConfigureAwait(false);

        // Clear tracked keys for this tenant
        ClearTenantKeys(tenantId);

        // Publish event for distributed invalidation
        await PublishInvalidationAsync(new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Tenant,
            TenantId = tenantId,
            OriginInstanceId = _instanceId
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task InvalidateUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating caches for user {UserId} in tenant {TenantId}", userId, tenantId);

        // Remove specific user cache entries
        var keyPattern = $"perm:{tenantId}:{userId}:";
        await _hybridCache.InvalidatePatternAsync(keyPattern, "permission", cancellationToken).ConfigureAwait(false);

        // Increment version for this tenant (affects all users, but simple)
        await _versionStore.IncrementVersionAsync(tenantId.ToString(), cancellationToken).ConfigureAwait(false);

        await PublishInvalidationAsync(new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.User,
            TenantId = tenantId,
            UserId = userId,
            OriginInstanceId = _instanceId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateResourceAsync(Guid tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating ACL caches for resource {ResourceType}:{ResourceId} in tenant {TenantId}", 
            resourceType, resourceId, tenantId);

        var keyPattern = $"acl:{tenantId}:*:{resourceType}:{resourceId}:";
        await _hybridCache.InvalidatePatternAsync(keyPattern, "acl", cancellationToken).ConfigureAwait(false);

        await PublishInvalidationAsync(new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Resource,
            TenantId = tenantId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            OriginInstanceId = _instanceId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidatePolicyAsync(Guid tenantId, string? policyName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating policy caches for tenant {TenantId}, policy {PolicyName}", tenantId, policyName ?? "all");

        var keyPattern = policyName != null 
            ? $"policy:{tenantId}:{policyName}:" 
            : $"policy:{tenantId}:";
        
        await _hybridCache.InvalidatePatternAsync(keyPattern, "policy", cancellationToken).ConfigureAwait(false);

        await PublishInvalidationAsync(new CacheInvalidationEvent
        {
            Type = CacheInvalidationType.Policy,
            TenantId = tenantId,
            PolicyName = policyName,
            OriginInstanceId = _instanceId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task PublishInvalidationAsync(CacheInvalidationEvent invalidationEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.UseDistributedCache || !_options.UsePubSubInvalidation)
        {
            // No distributed cache or pub/sub disabled
            return Task.CompletedTask;
        }

        // Note: Actual Redis pub/sub implementation would go here.
        // This requires StackExchange.Redis ISubscriber, which is beyond IDistributedCache.
        // For now, we log the intent and rely on TTL + version-based invalidation.
        
        _logger.LogDebug(
            "Would publish invalidation event: Type={Type}, TenantId={TenantId}, OriginInstance={Instance}",
            invalidationEvent.Type,
            invalidationEvent.TenantId,
            invalidationEvent.OriginInstanceId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void HandleInvalidationEvent(CacheInvalidationEvent invalidationEvent)
    {
        // Skip if this is our own event
        if (invalidationEvent.OriginInstanceId == _instanceId)
        {
            return;
        }

        _logger.LogDebug(
            "Handling invalidation event from instance {Instance}: Type={Type}, TenantId={TenantId}",
            invalidationEvent.OriginInstanceId,
            invalidationEvent.Type,
            invalidationEvent.TenantId);

        switch (invalidationEvent.Type)
        {
            case CacheInvalidationType.Tenant:
                ClearTenantKeys(invalidationEvent.TenantId);
                break;

            case CacheInvalidationType.User:
                // Clear user-specific keys from L1
                if (invalidationEvent.UserId.HasValue)
                {
                    var userPattern = $"perm:{invalidationEvent.TenantId}:{invalidationEvent.UserId}:";
                    ClearKeysMatchingPattern(invalidationEvent.TenantId, userPattern);
                }
                break;

            case CacheInvalidationType.Resource:
                if (!string.IsNullOrEmpty(invalidationEvent.ResourceType) && !string.IsNullOrEmpty(invalidationEvent.ResourceId))
                {
                    var resourcePattern = $":{invalidationEvent.ResourceType}:{invalidationEvent.ResourceId}:";
                    ClearKeysMatchingPattern(invalidationEvent.TenantId, resourcePattern);
                }
                break;

            case CacheInvalidationType.Policy:
                var policyPattern = invalidationEvent.PolicyName != null
                    ? $"policy:{invalidationEvent.TenantId}:{invalidationEvent.PolicyName}:"
                    : $"policy:{invalidationEvent.TenantId}:";
                ClearKeysMatchingPattern(invalidationEvent.TenantId, policyPattern);
                break;
        }
    }

    /// <summary>
    ///     Registers a cache key for tracking (enables efficient invalidation).
    /// </summary>
    public void TrackKey(Guid tenantId, string cacheKey)
    {
        lock (_keysLock)
        {
            if (!_tenantCacheKeys.TryGetValue(tenantId, out var keys))
            {
                keys = new HashSet<string>();
                _tenantCacheKeys[tenantId] = keys;
            }
            keys.Add(cacheKey);
        }
    }

    private void ClearTenantKeys(Guid tenantId)
    {
        lock (_keysLock)
        {
            if (_tenantCacheKeys.TryGetValue(tenantId, out var keys))
            {
                foreach (var key in keys)
                {
                    _memoryCache.Remove(key);
                    _metrics.RecordEviction(CacheLevel.L1, "all", "tenant_invalidation");
                }
                keys.Clear();
            }
        }
    }

    private void ClearKeysMatchingPattern(Guid tenantId, string pattern)
    {
        lock (_keysLock)
        {
            if (_tenantCacheKeys.TryGetValue(tenantId, out var keys))
            {
                var matchingKeys = keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var key in matchingKeys)
                {
                    _memoryCache.Remove(key);
                    keys.Remove(key);
                    _metrics.RecordEviction(CacheLevel.L1, "pattern", "pattern_invalidation");
                }
            }
        }
    }
}
