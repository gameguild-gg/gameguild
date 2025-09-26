using GameGuild.Core.Exceptions;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Service for tenant management operations
/// Implements hexagonal architecture as an adapter (implementation)
/// Follows SOLID principles and DRY patterns
/// </summary>
public class TenantService(ITenantRepository repository, ITenantCacheService cacheService, ILogger<TenantService> logger) : ITenantService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting all active tenants");

        var tenants = await repository.GetActiveTenantsAsync(cancellationToken);

        logger.LogDebug("Retrieved {Count} active tenants", tenants.Count);

        return tenants;
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant by ID: {TenantId}", tenantId);

        // Try cache first
        var cachedTenant = cacheService.GetTenantById(tenantId);

        if (cachedTenant != null)
        {
            logger.LogDebug("Found tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant != null) { logger.LogDebug("Found tenant in database: {TenantSlug}", tenant.Slug); }
        else { logger.LogDebug("Tenant not found: {TenantId}", tenantId); }

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) { return null; }

        logger.LogDebug("Getting tenant by slug: {TenantSlug}", slug);

        // Try cache first
        var cachedTenant = cacheService.GetTenantBySlug(slug);

        if (cachedTenant != null)
        {
            logger.LogDebug("Found tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        var tenant = await repository.GetBySlugAsync(slug, cancellationToken);

        if (tenant != null) { logger.LogDebug("Found tenant in database: {TenantSlug}", tenant.Slug); }
        else { logger.LogDebug("Tenant not found: {TenantSlug}", slug); }

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting default tenant");

        // Try cache first
        var cachedTenant = cacheService.GetDefaultTenant();

        if (cachedTenant != null)
        {
            logger.LogDebug("Found default tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        var tenant = await repository.GetDefaultAsync(cancellationToken);

        if (tenant != null) { logger.LogDebug("Found default tenant in database: {TenantSlug}", tenant.Slug); }
        else { logger.LogWarning("No default tenant found"); }

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant> CreateTenantAsync(string name, string slug, string? description = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Tenant slug cannot be null or empty", nameof(slug));

        logger.LogInformation("Creating tenant: {TenantName} with slug: {TenantSlug}", name, slug);

        // Check if slug is available
        var isAvailable = await IsSlugAvailableAsync(slug, cancellationToken: cancellationToken);

        if (!isAvailable) { throw new BusinessException($"Tenant slug '{slug}' is already in use"); }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            IsActive = true,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        tenant = await repository.CreateAsync(tenant, cancellationToken);

        logger.LogInformation("Created tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Refresh cache to include new tenant
        await cacheService.RefreshTenantAsync(tenant.Id, cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant> UpdateTenantAsync(Guid tenantId, string name, string? description = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name cannot be null or empty", nameof(name));

        logger.LogInformation("Updating tenant: {TenantId}", tenantId);

        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant == null) { throw new NotFoundException($"Tenant with ID {tenantId} not found"); }

        tenant.Update(name, description);

        tenant = await repository.UpdateAsync(tenant, cancellationToken);

        logger.LogInformation("Updated tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Refresh cache with updated tenant
        await cacheService.RefreshTenantAsync(tenantId, cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Activating tenant: {TenantId}", tenantId);

        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant == null) { throw new NotFoundException($"Tenant with ID {tenantId} not found"); }

        if (tenant.IsActive)
        {
            logger.LogDebug("Tenant is already active: {TenantId}", tenantId);

            return tenant;
        }

        tenant.Activate();

        tenant = await repository.UpdateAsync(tenant, cancellationToken);

        logger.LogInformation("Activated tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Refresh cache with updated tenant
        await cacheService.RefreshTenantAsync(tenantId, cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant> DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deactivating tenant: {TenantId}", tenantId);

        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant == null) { throw new NotFoundException($"Tenant with ID {tenantId} not found"); }

        if (tenant.IsDefault) { throw new BusinessException("Cannot deactivate the default tenant"); }

        if (!tenant.IsActive)
        {
            logger.LogDebug("Tenant is already inactive: {TenantId}", tenantId);

            return tenant;
        }

        tenant.Deactivate();

        tenant = await repository.UpdateAsync(tenant, cancellationToken);

        logger.LogInformation("Deactivated tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Refresh cache with updated tenant
        await cacheService.RefreshTenantAsync(tenantId, cancellationToken);

        return tenant;
    }

    /// <inheritdoc />
    public async Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting tenant: {TenantId}", tenantId);

        var tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant == null) { throw new NotFoundException($"Tenant with ID {tenantId} not found"); }

        if (tenant.IsDefault) { throw new BusinessException("Cannot delete the default tenant"); }

        await repository.DeleteAsync(tenantId, cancellationToken);

        logger.LogInformation("Deleted tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Remove from cache
        cacheService.InvalidateTenant(tenantId);
    }

    /// <inheritdoc />
    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant settings: {TenantId}", tenantId);

        // Try cache first
        var cachedSettings = cacheService.GetTenantSettings(tenantId);

        if (cachedSettings != null)
        {
            logger.LogDebug("Found tenant settings in cache: {TenantId}", tenantId);

            return cachedSettings;
        }

        // Fallback to database
        var settings = await repository.GetTenantSettingsAsync(tenantId, cancellationToken);

        if (settings != null) { logger.LogDebug("Found tenant settings in database: {TenantId}", tenantId); }

        return settings;
    }

    /// <inheritdoc />
    public async Task<TenantSettings> UpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        logger.LogInformation("Updating tenant settings: {TenantId}", tenantId);

        settings = await repository.CreateOrUpdateTenantSettingsAsync(tenantId, settings, cancellationToken);

        logger.LogInformation("Updated tenant settings: {TenantId}", tenantId);

        // Refresh cache with updated settings
        await cacheService.RefreshTenantSettingsAsync(tenantId, cancellationToken);

        return settings;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting tenant domains: {TenantId}", tenantId);

        // Try cache first
        var cachedDomains = cacheService.GetTenantDomains(tenantId);

        if (cachedDomains.Any())
        {
            logger.LogDebug("Found {Count} tenant domains in cache: {TenantId}", cachedDomains.Count, tenantId);

            return cachedDomains;
        }

        // Fallback to database
        var domains = await repository.GetTenantDomainsAsync(tenantId, cancellationToken);

        logger.LogDebug("Found {Count} tenant domains in database: {TenantId}", domains.Count, tenantId);

        return domains;
    }

    /// <inheritdoc />
    public async Task<TenantDomain> AddTenantDomainAsync(Guid tenantId, string topLevelDomain, string? subdomain = null, bool isMainDomain = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topLevelDomain)) throw new ArgumentException("Top-level domain cannot be null or empty", nameof(topLevelDomain));

        var domain = string.IsNullOrWhiteSpace(subdomain) ? topLevelDomain : $"{subdomain}.{topLevelDomain}";

        logger.LogInformation("Adding tenant domain: {TenantId} - {Domain}", tenantId, domain);

        // Check if domain already exists for this tenant
        var existingDomain = await repository.FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);

        if (existingDomain != null && existingDomain.TenantId == tenantId) { throw new BusinessException($"Domain '{domain}' already exists for this tenant"); }

        var tenantDomain = new TenantDomain { TenantId = tenantId, Subdomain = subdomain, TopLevelDomain = topLevelDomain, IsMainDomain = isMainDomain };

        tenantDomain = await repository.CreateTenantDomainAsync(tenantDomain, cancellationToken);

        logger.LogInformation("Added tenant domain: {TenantId} - {Domain}", tenantId, domain);

        // Refresh cache with new domain
        await cacheService.RefreshTenantDomainsAsync(tenantId, cancellationToken);

        return tenantDomain;
    }

    /// <inheritdoc />
    public async Task<TenantDomain?> FindTenantByDomainMatchAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) { return null; }

        logger.LogDebug("Finding tenant by domain match: {Email}", email);

        // Try cache first
        var cachedDomain = cacheService.FindTenantByDomainMatch(email);

        if (cachedDomain != null)
        {
            logger.LogDebug("Found tenant domain in cache for email: {Email}", email);

            return cachedDomain;
        }

        // Extract domain from email
        var atIndex = email.LastIndexOf('@');

        if (atIndex <= 0 || atIndex >= email.Length - 1) { return null; }

        var domain = email.Substring(atIndex + 1).ToLowerInvariant();

        // Check if it's a subdomain (contains dots)
        var parts = domain.Split('.');
        string? subdomain = null;
        string topLevelDomain = domain;

        if (parts.Length > 2)
        {
            subdomain = string.Join(".", parts.Take(parts.Length - 2));
            topLevelDomain = string.Join(".", parts.Skip(parts.Length - 2));
        }

        // Fallback to database - try exact match first
        var tenantDomain = await repository.FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);

        // If no exact match, try with just the top-level domain
        if (tenantDomain == null && subdomain != null)
        {
            tenantDomain = await repository.FindTenantDomainByMatchAsync(domain, null, cancellationToken);
        }

        if (tenantDomain != null) { logger.LogDebug("Found tenant domain in database for email: {Email}", email); }

        return tenantDomain;
    }

    /// <inheritdoc />
    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) { return false; }

        logger.LogDebug("Checking slug availability: {TenantSlug}", slug);

        var isAvailable = await repository.IsSlugAvailableAsync(slug, excludeTenantId, cancellationToken);

        logger.LogDebug("Slug {TenantSlug} availability: {IsAvailable}", slug, isAvailable);

        return isAvailable;
    }
}
