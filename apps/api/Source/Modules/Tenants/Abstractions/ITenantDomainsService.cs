namespace GameGuild.Modules.Tenants;

/// <summary>
///     Service interface for tenant domains management operations
///     Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ITenantDomainsService
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
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="isMainDomain">Whether this is the main domain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant domain</returns>
    Task<TenantDomain> CreateTenantDomainAsync(Guid tenantId, string topLevelDomain, string? subdomain = null, bool isMainDomain = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant domain
    /// </summary>
    /// <param name="domainId">The domain ID</param>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="isMainDomain">Whether this is the main domain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant domain</returns>
    Task<TenantDomain> UpdateTenantDomainAsync(Guid domainId, string topLevelDomain, string? subdomain = null, bool isMainDomain = false, CancellationToken cancellationToken = default);

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
    ///     Find tenant by domain match
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant associated with the domain or null if not found</returns>
    Task<Tenant?> FindTenantByDomainAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default);

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
    ///     Set a domain as the primary domain for a tenant (unsets others)
    /// </summary>
    /// <param name="domainId">The domain ID to set as primary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant domain</returns>
    Task<TenantDomain> SetPrimaryDomainAsync(Guid domainId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the primary domain for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The primary tenant domain or null if not found</returns>
    Task<TenantDomain?> GetPrimaryTenantDomainAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validate domain format and availability
    /// </summary>
    /// <param name="topLevelDomain">The top-level domain</param>
    /// <param name="subdomain">Optional subdomain</param>
    /// <param name="excludeDomainId">Optional domain ID to exclude from availability check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any errors</returns>
    Task<DomainValidationResult> ValidateDomainAsync(string topLevelDomain, string? subdomain = null, Guid? excludeDomainId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all domains across all tenants (for administrative purposes)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tenant domains</returns>
    Task<IReadOnlyList<TenantDomain>> GetAllTenantDomainsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Domain validation result
/// </summary>
public class DomainValidationResult
{
    public bool IsValid { get; set; }

    public bool IsAvailable { get; set; }

    public List<string> Errors { get; set; } = [];

    public static DomainValidationResult Success() => new() { IsValid = true, IsAvailable = true };

    public static DomainValidationResult Failure(params string[ ] errors) => new() { IsValid = false, IsAvailable = false, Errors = errors.ToList() };

    public static DomainValidationResult Unavailable(string message = "Domain is not available") => new() { IsValid = true, IsAvailable = false, Errors = [message] };
}
