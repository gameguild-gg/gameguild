using System.Collections.Concurrent;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Cached wrapper for IPolicyDefinitionStore that adds in-memory caching.
///     This wraps a database-backed store and provides fast reads via cache.
/// </summary>
public sealed class CachedPolicyDefinitionStore : IPolicyDefinitionStore
{
    private readonly IPolicyDefinitionStore _innerStore;
    private readonly IMemoryCache _cache;
    private readonly ITenantSecurityVersionStore _versionStore;
    private readonly AuthorizationCacheOptions _options;
    private readonly ConcurrentDictionary<string, HashSet<string>> _tenantCacheKeys = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="CachedPolicyDefinitionStore"/>.
    /// </summary>
    /// <param name="innerStore">The underlying store (typically database-backed).</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="versionStore">The version store for cache invalidation.</param>
    /// <param name="options">Cache configuration options.</param>
    public CachedPolicyDefinitionStore(
        IPolicyDefinitionStore innerStore,
        IMemoryCache cache,
        ITenantSecurityVersionStore versionStore,
        IOptions<AuthorizationCacheOptions> options)
    {
        _innerStore = innerStore;
        _cache = cache;
        _versionStore = versionStore;
        _options = options.Value;
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

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out PolicyDefinition? cachedPolicy))
        {
            return cachedPolicy;
        }

        // Cache miss - fetch from underlying store
        var policy = await _innerStore.GetPolicyAsync(policyName, tenantId, cancellationToken).ConfigureAwait(false);

        if (policy != null)
        {
            CachePolicy(cacheKey, effectiveTenantId, policy);
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

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<PolicyDefinition>? cachedPolicies))
        {
            return cachedPolicies!;
        }

        // Cache miss - fetch from underlying store
        var policies = await _innerStore.GetTenantPoliciesAsync(tenantId, cancellationToken).ConfigureAwait(false);

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds * 2))
            .SetSize(policies.Count);

        _cache.Set(cacheKey, policies, cacheOptions);
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
            foreach (var key in keys)
            {
                _cache.Remove(key);
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
                _cache.Remove(key);
                keys.Remove(key);
            }
        }
    }

    private static string BuildCacheKey(string policyName, string tenantId, long version)
    {
        return $"policy:{policyName}:{tenantId}:v{version}";
    }

    private void CachePolicy(string cacheKey, string tenantId, PolicyDefinition policy)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.PolicyTtlSeconds * 2))
            .SetSize(1);

        _cache.Set(cacheKey, policy, cacheOptions);
        TrackTenantCacheKey(tenantId, cacheKey);
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
