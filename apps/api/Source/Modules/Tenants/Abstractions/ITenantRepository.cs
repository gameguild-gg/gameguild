namespace GameGuild.Modules.Tenants;

/// <summary>
/// Repository interface for tenant data access operations
/// Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantRepository
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
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tenant by slug
    /// </summary>
    /// <param name="slug">The tenant slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get default tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default tenant or null if not found</returns>
    Task<Tenant?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new tenant
    /// </summary>
    /// <param name="tenant">The tenant to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant</returns>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing tenant
    /// </summary>
    /// <param name="tenant">The tenant to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant</returns>
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a tenant (soft delete)
    /// </summary>
    /// <param name="id">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a tenant slug is available
    /// </summary>
    /// <param name="slug">The slug to check</param>
    /// <param name="excludeId">Optional tenant ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slug is available</returns>
    Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    // === TENANT SETTINGS OPERATIONS ===

    /// <summary>
    /// Get tenant settings by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant settings or null if not found</returns>
    Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update tenant settings
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="settings">The settings to create or update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created or updated tenant settings</returns>
    Task<TenantSettings> CreateOrUpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default);

    // === TENANT DOMAINS OPERATIONS ===

    /// <summary>
    /// Get all domains for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new tenant domain
    /// </summary>
    /// <param name="tenantDomain">The tenant domain to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant domain</returns>
    Task<TenantDomain> CreateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find tenant domain by domain match
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching tenant domain or null if not found</returns>
    Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default);
}
