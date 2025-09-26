using System.Text.RegularExpressions;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Service implementation for tenant domains management operations
///     Follows hexagonal architecture principles as an adapter (implementation)
/// </summary>
public class TenantDomainsService(
    ITenantDomainsRepository repository,
    ITenantRepository tenantRepository,
    ITenantCacheService cacheService,
    ILogger<TenantDomainsService> logger) : ITenantDomainsService
{
    private static readonly Regex DomainPattern = new(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$", RegexOptions.Compiled);
    private static readonly Regex SubdomainPattern = new(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?$", RegexOptions.Compiled);

    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant domains for tenant: {TenantId}", tenantId);

        // Try cache first
        IReadOnlyList<TenantDomain>? cachedDomains = cacheService.GetTenantDomains(tenantId);
        if (cachedDomains != null)
        {
            logger.LogDebug("Found {Count} tenant domains in cache for tenant: {TenantId}", cachedDomains.Count, tenantId);
            return cachedDomains;
        }

        // Fallback to database
        IReadOnlyList<TenantDomain> domains = await repository.GetTenantDomainsAsync(tenantId, cancellationToken);

        logger.LogDebug("Found {Count} tenant domains in database for tenant: {TenantId}", domains.Count, tenantId);

        return domains;
    }

    public async Task<TenantDomain?> GetTenantDomainByIdAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant domain by ID: {DomainId}", domainId);

        TenantDomain? domain = await repository.GetTenantDomainByIdAsync(domainId, cancellationToken);

        if (domain != null)
        {
            logger.LogDebug("Found tenant domain: {FullDomain}", domain.FullDomainName);
        }
        else
        {
            logger.LogDebug("Tenant domain not found: {DomainId}", domainId);
        }

        return domain;
    }

    public async Task<TenantDomain> CreateTenantDomainAsync(Guid tenantId, string topLevelDomain, string? subdomain = null, bool isPrimary = false, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Creating tenant domain for tenant {TenantId}: {Subdomain}.{TopLevel}", tenantId, subdomain, topLevelDomain);

        // Validate domain
        DomainValidationResult validationResult = await ValidateDomainAsync(topLevelDomain, subdomain, cancellationToken: cancellationToken);
        if (!validationResult.IsValid || !validationResult.IsAvailable)
        {
            string errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid or unavailable domain: {errors}");
        }

        // Verify tenant exists
        Tenant? tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
        {
            throw new ArgumentException($"Tenant not found: {tenantId}");
        }

        // Create domain
        TenantDomain newDomain = new()
        {
            TenantId = tenantId,
            TopLevelDomain = topLevelDomain.ToLowerInvariant(),
            Subdomain = subdomain?.ToLowerInvariant(),
            IsPrimary = isPrimary
        };

        TenantDomain createdDomain = await repository.CreateTenantDomainAsync(newDomain, cancellationToken);

        // If this is primary, unset other primary domains for this tenant
        if (isPrimary)
        {
            await UnsetOtherPrimaryDomainsAsync(tenantId, createdDomain.Id, cancellationToken);
        }

        // Refresh cache
        await cacheService.RefreshTenantDomainsAsync(tenantId, cancellationToken);

        logger.LogInformation("Created tenant domain for tenant {TenantId}: {FullDomain}", tenantId, createdDomain.FullDomainName);

        return createdDomain;
    }

    public async Task<TenantDomain> UpdateTenantDomainAsync(Guid domainId, string topLevelDomain, string? subdomain = null, bool isPrimary = false, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Updating tenant domain {DomainId}: {Subdomain}.{TopLevel}", domainId, subdomain, topLevelDomain);

        // Get existing domain
        TenantDomain? existingDomain = await repository.GetTenantDomainByIdAsync(domainId, cancellationToken);
        if (existingDomain == null)
        {
            throw new ArgumentException($"Tenant domain not found: {domainId}");
        }

        // Validate domain (exclude current domain from availability check)
        DomainValidationResult validationResult = await ValidateDomainAsync(topLevelDomain, subdomain, domainId, cancellationToken);
        if (!validationResult.IsValid || !validationResult.IsAvailable)
        {
            string errors = string.Join(", ", validationResult.Errors);
            throw new ArgumentException($"Invalid or unavailable domain: {errors}");
        }

        // Update domain
        existingDomain.TopLevelDomain = topLevelDomain.ToLowerInvariant();
        existingDomain.Subdomain = subdomain?.ToLowerInvariant();
        existingDomain.IsPrimary = isPrimary;

        TenantDomain updatedDomain = await repository.UpdateTenantDomainAsync(existingDomain, cancellationToken);

        // If this is primary, unset other primary domains for this tenant
        if (isPrimary)
        {
            await UnsetOtherPrimaryDomainsAsync(existingDomain.TenantId, domainId, cancellationToken);
        }

        // Refresh cache
        await cacheService.RefreshTenantDomainsAsync(existingDomain.TenantId, cancellationToken);

        logger.LogInformation("Updated tenant domain {DomainId}: {FullDomain}", domainId, updatedDomain.FullDomainName);

        return updatedDomain;
    }

    public async Task<bool> DeleteTenantDomainAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Deleting tenant domain: {DomainId}", domainId);

        // Get domain to check if it exists and get tenant ID for cache refresh
        TenantDomain? domain = await repository.GetTenantDomainByIdAsync(domainId, cancellationToken);
        if (domain == null)
        {
            logger.LogDebug("Tenant domain not found for deletion: {DomainId}", domainId);
            return false;
        }

        bool deleted = await repository.DeleteTenantDomainAsync(domainId, cancellationToken);

        if (deleted)
        {
            // Refresh cache
            await cacheService.RefreshTenantDomainsAsync(domain.TenantId, cancellationToken);
            logger.LogInformation("Deleted tenant domain: {FullDomain}", domain.FullDomainName);
        }

        return deleted;
    }

    public async Task<TenantDomain?> FindTenantDomainByMatchAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding tenant domain by match: {Subdomain}.{TopLevel}", subdomain, topLevelDomain);

        TenantDomain? domain = await repository.FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);

        if (domain != null)
        {
            logger.LogDebug("Found tenant domain: {FullDomain} for tenant: {TenantId}", domain.FullDomainName, domain.TenantId);
        }
        else
        {
            logger.LogDebug("Tenant domain not found for: {Subdomain}.{TopLevel}", subdomain, topLevelDomain);
        }

        return domain;
    }

    public async Task<Tenant?> FindTenantByDomainAsync(string topLevelDomain, string? subdomain = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Finding tenant by domain: {Subdomain}.{TopLevel}", subdomain, topLevelDomain);

        TenantDomain? domain = await FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);
        if (domain == null)
        {
            return null;
        }

        Tenant? tenant = await tenantRepository.GetByIdAsync(domain.TenantId, cancellationToken);

        if (tenant != null)
        {
            logger.LogDebug("Found tenant {TenantSlug} for domain: {FullDomain}", tenant.Slug, domain.FullDomainName);
        }

        return tenant;
    }

    public async Task<bool> IsDomainAvailableAsync(string topLevelDomain, string? subdomain = null, Guid? excludeDomainId = null, CancellationToken cancellationToken = default)
    {
        return await repository.IsDomainAvailableAsync(topLevelDomain, subdomain, excludeDomainId, cancellationToken);
    }

    public async Task<TenantDomain> SetPrimaryDomainAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Setting primary domain: {DomainId}", domainId);

        // Get domain
        TenantDomain? domain = await repository.GetTenantDomainByIdAsync(domainId, cancellationToken);
        if (domain == null)
        {
            throw new ArgumentException($"Tenant domain not found: {domainId}");
        }

        // Set as primary
        domain.IsPrimary = true;
        TenantDomain updatedDomain = await repository.UpdateTenantDomainAsync(domain, cancellationToken);

        // Unset other primary domains for this tenant
        await UnsetOtherPrimaryDomainsAsync(domain.TenantId, domainId, cancellationToken);

        // Refresh cache
        await cacheService.RefreshTenantDomainsAsync(domain.TenantId, cancellationToken);

        logger.LogInformation("Set primary domain for tenant {TenantId}: {FullDomain}", domain.TenantId, updatedDomain.FullDomainName);

        return updatedDomain;
    }

    public async Task<TenantDomain?> GetPrimaryTenantDomainAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting primary tenant domain for tenant: {TenantId}", tenantId);

        IReadOnlyList<TenantDomain> domains = await GetTenantDomainsAsync(tenantId, cancellationToken);
        TenantDomain? primaryDomain = domains.FirstOrDefault(d => d.IsPrimary);

        if (primaryDomain != null)
        {
            logger.LogDebug("Found primary domain for tenant {TenantId}: {FullDomain}", tenantId, primaryDomain.FullDomainName);
        }
        else
        {
            logger.LogDebug("No primary domain found for tenant: {TenantId}", tenantId);
        }

        return primaryDomain;
    }

    public async Task<DomainValidationResult> ValidateDomainAsync(string topLevelDomain, string? subdomain = null, Guid? excludeDomainId = null, CancellationToken cancellationToken = default)
    {
        List<string> errors = new();

        // Validate top-level domain format
        if (string.IsNullOrWhiteSpace(topLevelDomain))
        {
            errors.Add("Top-level domain is required");
        }
        else if (!DomainPattern.IsMatch(topLevelDomain))
        {
            errors.Add("Invalid top-level domain format");
        }

        // Validate subdomain format if provided
        if (!string.IsNullOrWhiteSpace(subdomain) && !SubdomainPattern.IsMatch(subdomain))
        {
            errors.Add("Invalid subdomain format");
        }

        // If format validation failed, return early
        if (errors.Count > 0)
        {
            return DomainValidationResult.Failure(errors.ToArray());
        }

        // Check availability
        bool isAvailable = await IsDomainAvailableAsync(topLevelDomain, subdomain, excludeDomainId, cancellationToken);
        if (!isAvailable)
        {
            return DomainValidationResult.Unavailable();
        }

        return DomainValidationResult.Success();
    }

    public async Task<IReadOnlyList<TenantDomain>> GetAllTenantDomainsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all tenant domains");

        IReadOnlyList<TenantDomain> allDomains = await repository.GetAllTenantDomainsAsync(cancellationToken);

        logger.LogDebug("Retrieved {Count} tenant domains", allDomains.Count);

        return allDomains;
    }

    private async Task UnsetOtherPrimaryDomainsAsync(Guid tenantId, Guid excludeDomainId, CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantDomain> domains = await repository.GetTenantDomainsAsync(tenantId, cancellationToken);

        foreach (TenantDomain domain in domains.Where(d => d.IsPrimary && d.Id != excludeDomainId))
        {
            domain.IsPrimary = false;
            await repository.UpdateTenantDomainAsync(domain, cancellationToken);
        }
    }
}