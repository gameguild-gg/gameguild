using System.Collections.Concurrent;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Cached wrapper for IPolicyDefinitionStore that adds hybrid (L1 + L2) caching.
///     This wraps a database-backed store and provides fast reads via cache.
/// </summary>
/// <remarks>
///     <para>
///         <b>Cache Levels:</b>
///         <list type="bullet">
///             <item>L1 (IMemoryCache): Fast, per-instance cache with short TTL</item>
///             <item>L2 (IDistributedCache via IHybridPermissionCache): Shared cache for multi-instance deployments</item>
///         </list>
///     </para>
/// </remarks>
public sealed class CachedPolicyDefinitionStore : IPolicyDefinitionStore
{
    private const string CacheType = "policy";
    
    private readonly IPolicyDefinitionStore _innerStore;
    private readonly IMemoryCache _l1Cache;
    private readonly IHybridPermissionCache? _hybridCache;
    private readonly ITenantSecurityVersionStore _versionStore;
    private readonly ICacheMetricsService? _metrics;
    private readonly AuthorizationCacheOptions _options;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tenantCacheKeys = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="CachedPolicyDefinitionStore"/>.
    /// </summary>
    /// <param name="innerStore">The underlying store (typically database-backed).</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="versionStore">The version store for cache invalidation.</param>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="hybridCache">Optional hybrid cache for L2 distributed caching.</param>
    /// <param name="metrics">Optional cache metrics service.</param>
    public CachedPolicyDefinitionStore(
        IPolicyDefinitionStore innerStore,
        IMemoryCache cache,
        ITenantSecurityVersionStore versionStore,
        IOptions<AuthorizationCacheOptions> options,
        IHybridPermissionCache? hybridCache = null,
        ICacheMetricsService? metrics = null)
    {
        _innerStore = innerStore;
        _l1Cache = cache;
        _versionStore = versionStore;
        _options = options.Value;
        _hybridCache = hybridCache;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async Task<PolicyDefinition?> GetPolicyAsync(
        string policyName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTenantId = tenantId ?? "global";
        var version = await _versionStore.GetVersionAsync(effectiveTenantId, cancellationToken).ConfigureAwait(false);
        var cacheKey = BuildCacheKey(policyName, effectiveTenantId, version);

        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out PolicyDefinition? cachedPolicy))
        {
            _metrics?.RecordHit(CacheLevel.L1, CacheType);
            return cachedPolicy;
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetAsync<PolicyDefinition>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult != null)
            {
                _metrics?.RecordHit(CacheLevel.L2, CacheType);
                // Promote to L1
                CachePolicy(cacheKey, effectiveTenantId, hybridResult, l1Only: true);
                return hybridResult;
            }
        }

        // Cache miss - fetch from underlying store
        _metrics?.RecordMiss(CacheType);
        var policy = await _innerStore.GetPolicyAsync(policyName, tenantId, cancellationToken).ConfigureAwait(false);

        if (policy != null)
        {
            await CachePolicyAsync(cacheKey, effectiveTenantId, policy, cancellationToken).ConfigureAwait(false);
        }

        return policy;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDefinition>> GetTenantPoliciesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var version = await _versionStore.GetVersionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var cacheKey = $"tenant_policies:{tenantId}:v{version}";

        // Try L1 cache first
        if (_l1Cache.TryGetValue(cacheKey, out IReadOnlyList<PolicyDefinition>? cachedPolicies))
        {
            _metrics?.RecordHit(CacheLevel.L1, CacheType);
            return cachedPolicies!;
        }

        // Try L2 (hybrid) cache if available
        if (_hybridCache != null)
        {
            var hybridResult = await _hybridCache.GetAsync<List<PolicyDefinition>>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
            if (hybridResult != null)
            {
                _metrics?.RecordHit(CacheLevel.L2, CacheType);
                // Promote to L1
                CacheTenantPolicies(cacheKey, tenantId, hybridResult, l1Only: true);
                return hybridResult;
            }
        }

        // Cache miss - fetch from underlying store
        _metrics?.RecordMiss(CacheType);
        var policies = await _innerStore.GetTenantPoliciesAsync(tenantId, cancellationToken).ConfigureAwait(false);

        await CacheTenantPoliciesAsync(cacheKey, tenantId, policies, cancellationToken).ConfigureAwait(false);

        return policies;
    }

    /// <inheritdoc />
    public async Task<long> GetVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Version is always fetched from the version store (which may have its own caching)
        return await _versionStore.GetVersionAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Invalidates all cached policies for a tenant.
    ///     Should be called when policies are updated.
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
    ///     Invalidates all cached policies for a tenant asynchronously,
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

    /// <summary>
    ///     Invalidates a specific policy in the cache.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">The tenant ID.</param>
    public void InvalidatePolicy(string policyName, string tenantId)
    {
        // Since cache keys include version, incrementing version effectively invalidates
        // But we can also explicitly remove known keys
        if (_tenantCacheKeys.TryGetValue(tenantId, out var keys))
        {
            var keysToRemove = keys.Where(k => k.Contains($"policy:{policyName}:")).ToList();
            foreach (var key in keysToRemove)
            {
                _l1Cache.Remove(key);
                _metrics?.RecordEviction(CacheLevel.L1, CacheType);
                keys.Remove(key);
            }
        }
    }

    private static string BuildCacheKey(string policyName, string tenantId, long version)
    {
        return $"policy:{policyName}:{tenantId}:v{version}";
    }

    private void CachePolicy(string cacheKey, string tenantId, PolicyDefinition policy, bool l1Only)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds * 2))
            .SetSize(1);

        _l1Cache.Set(cacheKey, policy, cacheOptions);
        TrackTenantCacheKey(tenantId, cacheKey);
    }

    private async Task CachePolicyAsync(string cacheKey, string tenantId, PolicyDefinition policy, CancellationToken cancellationToken)
    {
        // Cache in L1
        CachePolicy(cacheKey, tenantId, policy, l1Only: true);

        // Cache in L2 if available
        if (_hybridCache != null)
        {
            await _hybridCache.SetAsync(cacheKey, policy, CacheType, cancellationToken).ConfigureAwait(false);
        }
    }

    private void CacheTenantPolicies(string cacheKey, string tenantId, IReadOnlyList<PolicyDefinition> policies, bool l1Only)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds * 2))
            .SetSize(policies.Count);

        _l1Cache.Set(cacheKey, policies, cacheOptions);
        TrackTenantCacheKey(tenantId, cacheKey);
    }

    private async Task CacheTenantPoliciesAsync(string cacheKey, string tenantId, IReadOnlyList<PolicyDefinition> policies, CancellationToken cancellationToken)
    {
        // Cache in L1
        CacheTenantPolicies(cacheKey, tenantId, policies, l1Only: true);

        // Cache in L2 if available (serialize as List for proper deserialization)
        if (_hybridCache != null)
        {
            await _hybridCache.SetAsync(cacheKey, policies.ToList(), CacheType, cancellationToken).ConfigureAwait(false);
        }
    }

    private void TrackTenantCacheKey(string tenantId, string cacheKey)
    {
        var keys = _tenantCacheKeys.GetOrAdd(tenantId, _ => []);
        lock (keys)
        {
            keys.Add(cacheKey);
        }
    }
}
