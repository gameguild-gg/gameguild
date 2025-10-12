namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository interface for tenant domains data access operations
///     Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantDomainsRepository
{
    /// <summary>
    ///     Get all domains for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a specific tenant domain by ID
    /// </summary>
    /// <param name="domainId">The domain ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant domain or null if not found</returns>
    Task<TenantDomain?> GetTenantDomainByIdAsync(Guid domainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new tenant domain
    /// </summary>
    /// <param name="tenantDomain">The tenant domain to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant domain</returns>
    Task<TenantDomain> CreateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant domain
    /// </summary>
    /// <param name="tenantDomain">The tenant domain to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant domain</returns>
    Task<TenantDomain> UpdateTenantDomainAsync(TenantDomain tenantDomain, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant domain
    /// </summary>
    /// <param name="domainId">The domain ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if domain was deleted, false if not found</returns>
    Task<bool> DeleteTenantDomainAsync(Guid domainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Find tenant domain by domain match
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The matching tenant domain or null if not found</returns>
    Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a domain combination is available
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="excludeDomainId">Optional domain ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the domain combination is available</returns>
    Task<bool> IsDomainAvailableAsync(string topLevelDomain, string? subdomain = null, Guid? excludeDomainId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all domains across all tenants (for administrative purposes)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetAllTenantDomainsAsync(CancellationToken cancellationToken = default);
}
