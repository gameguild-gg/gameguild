using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Repository interface for tenant domain data access operations
/// </summary>
public interface ITenantDomainsRepository
{
    /// <summary>
    ///     Get tenant domain by ID
    /// </summary>
    /// <param name="id">The domain ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant domain or null if not found</returns>
    Task<TenantDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant domains by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant domain by full domain name
    /// </summary>
    /// <param name="domain">The full domain name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant domain or null if not found</returns>
    Task<TenantDomain?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get main domain for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The main tenant domain or null if not found</returns>
    Task<TenantDomain?> GetMainDomainAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new tenant domain
    /// </summary>
    /// <param name="domain">The tenant domain to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant domain</returns>
    Task<TenantDomain> CreateAsync(TenantDomain domain, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant domain
    /// </summary>
    /// <param name="domain">The tenant domain to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant domain</returns>
    Task<TenantDomain> UpdateAsync(TenantDomain domain, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant domain
    /// </summary>
    /// <param name="id">The domain ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if domain exists
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">The subdomain (optional)</param>
    /// <param name="excludeId">Optional domain ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the domain exists</returns>
    Task<bool> DomainExistsAsync(string topLevelDomain, string? subdomain = null, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
