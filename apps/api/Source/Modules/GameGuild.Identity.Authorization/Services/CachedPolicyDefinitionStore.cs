using System.Collections.Concurrent;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Cached wrapper for IPolicyDefinitionStore that adds hybrid (L1 + L2) caching.
///     This wraps a database-backed store and provides fast reads via cache.
///     Uses <see cref="HybridCacheHelper"/> for consolidated caching logic.
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
    private readonly HybridCacheHelper _cacheHelper;
    private readonly ITenantSecurityVersionStore _versionStore;
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
        _versionStore = versionStore;
        _cacheHelper = new HybridCacheHelper(cache, options, hybridCache, metrics);
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

        // Try cache first via helper
        var cacheResult = await _cacheHelper.GetAsync<PolicyDefinition>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
        if (cacheResult.Found)
        {
            TrackTenantCacheKey(effectiveTenantId, cacheKey);
            return cacheResult.Value;
        }

        // Cache miss - fetch from underlying store
        var policy = await _innerStore.GetPolicyAsync(policyName, tenantId, cancellationToken).ConfigureAwait(false);

        if (policy != null)
        {
            await _cacheHelper.SetAsync(cacheKey, policy, CacheType, cancellationToken).ConfigureAwait(false);
            TrackTenantCacheKey(effectiveTenantId, cacheKey);
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

        // Try cache first via helper
        var cacheResult = await _cacheHelper.GetAsync<List<PolicyDefinition>>(cacheKey, CacheType, cancellationToken).ConfigureAwait(false);
        if (cacheResult.Found)
        {
            TrackTenantCacheKey(tenantId, cacheKey);
            return cacheResult.Value!;
        }

        // Cache miss - fetch from underlying store
        var policies = await _innerStore.GetTenantPoliciesAsync(tenantId, cancellationToken).ConfigureAwait(false);

        await _cacheHelper.SetAsync(cacheKey, policies.ToList(), CacheType, cancellationToken).ConfigureAwait(false);
        TrackTenantCacheKey(tenantId, cacheKey);

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
            _cacheHelper.RemoveL1Many(keys, CacheType);
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
        if (_tenantCacheKeys.TryRemove(tenantId, out var keys))
        {
            foreach (var key in keys.ToList())
            {
                await _cacheHelper.RemoveAsync(key, CacheType, cancellationToken).ConfigureAwait(false);
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
            _cacheHelper.RemoveL1Many(keysToRemove, CacheType);
            foreach (var key in keysToRemove)
            {
                keys.Remove(key);
            }
        }
    }

    private static string BuildCacheKey(string policyName, string tenantId, long version)
    {
        return $"policy:{policyName}:{tenantId}:v{version}";
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
