using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Caches compiled authorization policies with tenant-aware invalidation.
/// </summary>
public interface IPolicyCache
{
    /// <summary>
    ///     Gets a cached policy by name.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">The tenant ID for cache key scoping.</param>
    /// <param name="version">The expected version for cache validation.</param>
    /// <returns>The cached policy or null if not found or stale.</returns>
    AuthorizationPolicy? Get(string policyName, string tenantId, long version);

    /// <summary>
    ///     Caches a compiled policy.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">The tenant ID for cache key scoping.</param>
    /// <param name="version">The version for cache invalidation.</param>
    /// <param name="policy">The compiled policy to cache.</param>
    void Set(string policyName, string tenantId, long version, AuthorizationPolicy policy);

    /// <summary>
    ///     Invalidates all cached policies for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    void Invalidate(string tenantId);

    /// <summary>
    ///     Invalidates a specific policy for a tenant.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">The tenant ID.</param>
    void Invalidate(string policyName, string tenantId);
}
