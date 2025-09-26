namespace GameGuild.Modules.Tenants;

/// <summary>
/// Interface for tenant caching operations
/// Provides high-performance access to tenant data with automatic cache management
/// </summary>
public interface ITenantCacheService
{
    /// <summary>
    /// Initialize the cache with tenant data from the database
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    Task InitializeCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant by ID from cache
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The tenant or null if not found</returns>
    Tenant? GetTenantById(Guid tenantId);

    /// <summary>
    /// Get tenant by slug from cache
    /// </summary>
    /// <param name="slug">The tenant slug</param>
    /// <returns>The tenant or null if not found</returns>
    Tenant? GetTenantBySlug(string slug);

    /// <summary>
    /// Get the default tenant from cache
    /// </summary>
    /// <returns>The default tenant or null if not found</returns>
    Tenant? GetDefaultTenant();

    /// <summary>
    /// Get tenant settings by tenant ID from cache
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The tenant settings or null if not found</returns>
    TenantSettings? GetTenantSettings(Guid tenantId);

    /// <summary>
    /// Get tenant domains by tenant ID from cache
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>List of tenant domains</returns>
    IReadOnlyList<TenantDomain> GetTenantDomains(Guid tenantId);

    /// <summary>
    /// Get the main tenant domain for a tenant from cache
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The main tenant domain or null if not found</returns>
    TenantDomain? GetMainTenantDomain(Guid tenantId);

    /// <summary>
    /// Find a tenant by domain match from cache
    /// </summary>
    /// <param name="email">The email to match against domains</param>
    /// <returns>The matching tenant domain or null if not found</returns>
    TenantDomain? FindTenantByDomainMatch(string email);

    /// <summary>
    /// Get all active tenants from cache
    /// </summary>
    /// <returns>List of active tenants</returns>
    IReadOnlyList<Tenant> GetActiveTenants();

    /// <summary>
    /// Refresh the cache by reloading data from the database
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate a specific tenant from cache (to force reload on next access)
    /// </summary>
    /// <param name="tenantId">The tenant ID to invalidate</param>
    void InvalidateTenant(Guid tenantId);

    /// <summary>
    /// Clear all cached data
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Cache statistics information</returns>
    TenantCacheStatistics GetCacheStatistics();

    /// <summary>
    /// Refresh cache after tenant entity changes
    /// </summary>
    /// <param name="tenantId">The tenant ID that was modified</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task RefreshTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cache after tenant settings changes
    /// </summary>
    /// <param name="tenantId">The tenant ID whose settings were modified</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task RefreshTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh cache after tenant domain changes
    /// </summary>
    /// <param name="tenantId">The tenant ID whose domains were modified</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task RefreshTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh entire cache (use sparingly)
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    Task RefreshAllAsync(CancellationToken cancellationToken = default);
}
