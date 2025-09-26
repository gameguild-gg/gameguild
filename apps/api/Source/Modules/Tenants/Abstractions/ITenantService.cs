namespace GameGuild.Modules.Tenants;

/// <summary>
/// Interface for tenant management operations
/// Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Get all active tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active tenants</returns>
    Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant by slug
    /// </summary>
    /// <param name="slug">The tenant slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default tenant or null if not found</returns>
    Task<Tenant?> GetDefaultTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new tenant
    /// </summary>
    /// <param name="name">The tenant name</param>
    /// <param name="slug">The tenant slug</param>
    /// <param name="description">The tenant description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant</returns>
    Task<Tenant> CreateTenantAsync(string name, string slug, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="name">The new name</param>
    /// <param name="description">The new description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant</returns>
    Task<Tenant> UpdateTenantAsync(Guid tenantId, string name, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The activated tenant</returns>
    Task<Tenant> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deactivated tenant</returns>
    Task<Tenant> DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a tenant (soft delete)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant settings or null if not found</returns>
    Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="settings">The updated settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant settings</returns>
    Task<TenantSettings> UpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant domains
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a domain to a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="isMainDomain">Whether this is the main domain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant domain</returns>
    Task<TenantDomain> AddTenantDomainAsync(Guid tenantId, string topLevelDomain, string? subdomain = null, bool isMainDomain = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find tenant by domain match
    /// </summary>
    /// <param name="email">The email to match against domains</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching tenant domain or null if not found</returns>
    Task<TenantDomain?> FindTenantByDomainMatchAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a tenant slug is available
    /// </summary>
    /// <param name="slug">The slug to check</param>
    /// <param name="excludeTenantId">Optional tenant ID to exclude from the check</param>  
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slug is available</returns>
    Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);
}
