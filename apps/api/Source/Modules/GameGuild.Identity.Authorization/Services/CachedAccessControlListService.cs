using System.Collections.Concurrent;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Cached wrapper for IAccessControlListService that adds hybrid (L1 + L2) caching for Access Control List lookups.
///     This wraps a database-backed service and provides fast reads via cache.
///     Write operations go through to the database and invalidate cache.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cache Levels:</b>
///         <list type="bullet">
///             <item>L1 (IMemoryCache): Fast, per-instance cache with short TTL</item>
///             <item>L2 (IDistributedCache via IHybridPermissionCache): Shared cache for multi-instance deployments</item>
///         </list>
///     </para>
///     <para>
///         <b>Cache Invalidation:</b>
///         Uses version-based cache keys. When permissions change, the tenant security version is incremented,
///         causing old cache entries to become stale. Explicit invalidation is also performed for immediate consistency.
///     </para>
/// </remarks>
public sealed class CachedAccessControlListService : IAccessControlListService
{
    private const string CacheType = "acl";
    
    private readonly IAccessControlListService _innerService;
    private readonly IMemoryCache _l1Cache;
    private readonly IHybridPermissionCache? _hybridCache;
    private readonly ITenantSecurityVersionStore _versionStore;
    private readonly ICacheMetricsService? _metrics;
    private readonly AuthorizationCacheOptions _options;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tenantCacheKeys = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="CachedAccessControlListService"/>.
    /// </summary>
    public CachedAccessControlListService(
        IAccessControlListService innerService,
        IMemoryCache cache,
        ITenantSecurityVersionStore versionStore,
        IOptions<AuthorizationCacheOptions> options,
        IHybridPermissionCache? hybridCache = null,
        ICacheMetricsService? metrics = null)
    {
        _innerService = innerService;
        _l1Cache = cache;
        _versionStore = versionStore;
        _options = options.Value;
        _hybridCache = hybridCache;
        _metrics = metrics;
    }

    #region Subject-based operations (preferred)

    /// <inheritdoc />
    public async Task<AccessLevel> EvaluateAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var version = await _versionStore.GetVersionAsync(tenantId.ToString(), cancellationToken).ConfigureAwait(false);
        var cacheKey = BuildSubjectCacheKey(subject, tenantId, resourceType, resourceId, version);

        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out AccessLevel cachedLevel))
        {
            _metrics?.RecordHit(CacheLevel.L1, CacheType);
            return cachedLevel;
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetValueAsync<AccessLevel>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult.Found)
            {
                // Promote to L1
                CacheAccessLevel(cacheKey, tenantId.ToString(), hybridResult.Value, l1Only: true);
                return hybridResult.Value;
            }
        }

        // Cache miss - fetch from underlying service
        _metrics?.RecordMiss(CacheType);
        var level = await _innerService.EvaluateAccessAsync(subject, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);

        await CacheAccessLevelAsync(cacheKey, tenantId.ToString(), level, cancellationToken).ConfigureAwait(false);

        return level;
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default)
    {
        var actualLevel = await EvaluateAccessAsync(subject, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);
        return actualLevel >= requiredLevel;
    }

    /// <inheritdoc />
    public async Task GrantAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        // Write through to underlying service
        await _innerService.GrantAccessAsync(grantorId, principalType, principalId, tenantId, resourceType, resourceId, accessLevel, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this principal/resource combination
        InvalidatePrincipalResourceCache(principalType, principalId, tenantId, resourceType, resourceId);
    }

    /// <inheritdoc />
    public async Task DenyAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        // Write through to underlying service
        await _innerService.DenyAccessAsync(grantorId, principalType, principalId, tenantId, resourceType, resourceId, accessLevel, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this principal/resource combination
        InvalidatePrincipalResourceCache(principalType, principalId, tenantId, resourceType, resourceId);
    }

    /// <inheritdoc />
    public async Task RevokeAccessAsync(
        Guid revokerId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        // Write through to underlying service
        await _innerService.RevokeAccessAsync(revokerId, principalType, principalId, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this principal/resource combination
        InvalidatePrincipalResourceCache(principalType, principalId, tenantId, resourceType, resourceId);
    }

    #endregion

    #region Legacy user-based operations (backward compatibility)

    /// <inheritdoc />
    public async Task<AccessLevel> GetAccessLevelAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var version = await _versionStore.GetVersionAsync(tenantId.ToString(), cancellationToken).ConfigureAwait(false);
        var cacheKey = BuildCacheKey(userId, tenantId, resourceType, resourceId, version);

        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out AccessLevel cachedLevel))
        {
            _metrics?.RecordHit(CacheLevel.L1, CacheType);
            return cachedLevel;
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetValueAsync<AccessLevel>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult.Found)
            {
                // Promote to L1
                CacheAccessLevel(cacheKey, tenantId.ToString(), hybridResult.Value, l1Only: true);
                return hybridResult.Value;
            }
        }

        // Cache miss - fetch from underlying service
        _metrics?.RecordMiss(CacheType);
        var level = await _innerService.GetAccessLevelAsync(userId, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);

        await CacheAccessLevelAsync(cacheKey, tenantId.ToString(), level, cancellationToken).ConfigureAwait(false);

        return level;
    }

    /// <inheritdoc />
    public async Task GrantAccessAsync(
        Guid grantorId,
        Guid granteeId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        // Write through to underlying service
        await _innerService.GrantAccessAsync(grantorId, granteeId, tenantId, resourceType, resourceId, accessLevel, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this user/resource combination
        InvalidateUserResourceCache(granteeId, tenantId, resourceType, resourceId);
    }

    /// <inheritdoc />
    public async Task RevokeAccessAsync(
        Guid revokerId,
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        // Write through to underlying service
        await _innerService.RevokeAccessAsync(revokerId, userId, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);

        // Invalidate cache for this user/resource combination
        InvalidateUserResourceCache(userId, tenantId, resourceType, resourceId);
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default)
    {
        var actualLevel = await GetAccessLevelAsync(userId, tenantId, resourceType, resourceId, cancellationToken).ConfigureAwait(false);
        return actualLevel >= requiredLevel;
    }

    #endregion

    /// <summary>
    ///     Invalidates all cached Access Control List entries for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    public void InvalidateTenant(string tenantId)
    {
        if (_tenantCacheKeys.TryRemove(tenantId, out var keys))
        {
            foreach (var key in keys)
            {
                _l1Cache.Remove(key);
                _metrics?.RecordEviction(CacheLevel.L1, CacheType);
            }
        }
    }

    /// <summary>
    ///     Invalidates all cached Access Control List entries for a tenant asynchronously,
    ///     including distributed cache if enabled.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Invalidate L1 cache
        InvalidateTenant(tenantId);

        // Invalidate L2 cache if available
        if (_hybridCache != null && _tenantCacheKeys.TryGetValue(tenantId, out var keys))
        {
            foreach (var key in keys.ToList())
            {
                await _hybridCache.RemoveAsync(key, CacheType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string BuildCacheKey(Guid userId, Guid tenantId, string resourceType, string resourceId, long version)
    {
        return $"acl:{tenantId}:{userId}:{resourceType}:{resourceId}:v{version}";
    }

    private static string BuildSubjectCacheKey(AclSubject subject, Guid tenantId, string resourceType, string resourceId, long version)
    {
        // Build a stable cache key from subject principals
        var userPart = subject.UserId?.ToString() ?? "anon";
        var rolesPart = subject.RoleIds.Count > 0 ? string.Join(",", subject.RoleIds.OrderBy(r => r)) : "nr";
        var groupsPart = subject.GroupIds.Count > 0 ? string.Join(",", subject.GroupIds.OrderBy(g => g)) : "ng";
        return $"acl:subj:{tenantId}:{userPart}:{rolesPart}:{groupsPart}:{resourceType}:{resourceId}:v{version}";
    }

    /// <summary>
    ///     Caches an access level in L1 cache only (used for L2 → L1 promotion).
    /// </summary>
    private void CacheAccessLevel(string cacheKey, string tenantId, AccessLevel level, bool l1Only)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.AccessControlListTtlSeconds))
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.AccessControlListTtlSeconds / 2));

        _l1Cache.Set(cacheKey, level, cacheOptions);

        // Track cache key for tenant invalidation
        TrackCacheKey(tenantId, cacheKey);
    }

    /// <summary>
    ///     Caches an access level in both L1 and L2 caches asynchronously.
    /// </summary>
    private async Task CacheAccessLevelAsync(string cacheKey, string tenantId, AccessLevel level, CancellationToken cancellationToken)
    {
        // Cache in L1
        CacheAccessLevel(cacheKey, tenantId, level, l1Only: true);

        // Cache in L2 if available
        if (_hybridCache != null)
        {
            await _hybridCache.SetValueAsync(cacheKey, level, CacheType, cancellationToken).ConfigureAwait(false);
        }
    }

    private void TrackCacheKey(string tenantId, string cacheKey)
    {
        _tenantCacheKeys.AddOrUpdate(
            tenantId,
            _ => new HashSet<string> { cacheKey },
            (_, existingKeys) =>
            {
                lock (existingKeys)
                {
                    existingKeys.Add(cacheKey);
                }
                return existingKeys;
            });
    }

    // ReSharper disable UnusedParameter.Local - Parameters reserved for future fine-grained cache invalidation
    private void InvalidatePrincipalResourceCache(AclPrincipalType principalType, Guid? principalId, Guid tenantId, string resourceType, string resourceId)
    // ReSharper restore UnusedParameter.Local
    {
        // When a principal's access changes, we need to invalidate any subject cache that might include this principal.
        // Since subject cache keys include multiple principals, we use a more aggressive invalidation strategy.
        var tenantIdString = tenantId.ToString();
        if (_tenantCacheKeys.TryGetValue(tenantIdString, out var keys))
        {
            // Look for any cache key containing this resource and potentially this principal
            var resourcePattern = $":{resourceType}:{resourceId}:";
            var keysToRemove = keys.Where(k => k.Contains(resourcePattern, StringComparison.OrdinalIgnoreCase)).ToList();

            lock (keys)
            {
                foreach (var key in keysToRemove)
                {
                    _l1Cache.Remove(key);
                    _metrics?.RecordEviction(CacheLevel.L1, CacheType);
                    keys.Remove(key);
                }
            }
        }
    }

    private void InvalidateUserResourceCache(Guid userId, Guid tenantId, string resourceType, string resourceId)
    {
        // Since we include version in the cache key, the cache will naturally become invalid
        // when the version is incremented by the underlying service.
        // However, we can also proactively remove known keys.
        var tenantIdString = tenantId.ToString();
        if (_tenantCacheKeys.TryGetValue(tenantIdString, out var keys))
        {
            var pattern = $"acl:{tenantId}:{userId}:{resourceType}:{resourceId}:";
            var keysToRemove = keys.Where(k => k.StartsWith(pattern, StringComparison.OrdinalIgnoreCase)).ToList();

            lock (keys)
            {
                foreach (var key in keysToRemove)
                {
                    _l1Cache.Remove(key);
                    _metrics?.RecordEviction(CacheLevel.L1, CacheType);
                    keys.Remove(key);
                }
            }
        }
    }
}
