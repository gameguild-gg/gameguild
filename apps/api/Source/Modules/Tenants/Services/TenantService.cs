using GameGuild.Core.Exceptions;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Service for tenant management operations
///     Implements hexagonal architecture as an adapter (implementation)
///     Follows SOLID principles and DRY patterns
/// </summary>
public class TenantService(
    ITenantRepository repository,
    ITenantSettingsService tenantSettingsService,
    ITenantDomainsService tenantDomainsService,
    ITenantCacheService cacheService,
    ILogger<TenantService> logger
) : ITenantService
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
        Tenant? cachedTenant = cacheService.GetTenantById(tenantId);

        if (cachedTenant != null)
        {
            logger.LogDebug("Found tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        Tenant? tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

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
        Tenant? cachedTenant = cacheService.GetTenantBySlug(slug);

        if (cachedTenant != null)
        {
            logger.LogDebug("Found tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        Tenant? tenant = await repository.GetBySlugAsync(slug, cancellationToken);

        if (tenant != null) { logger.LogDebug("Found tenant in database: {TenantSlug}", tenant.Slug); }
        else { logger.LogDebug("Tenant not found: {TenantSlug}", slug); }

        return tenant;
    }

    /// <inheritdoc />
    public async Task<Tenant?> GetDefaultTenantAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting default tenant");

        // Try cache first
        Tenant? cachedTenant = cacheService.GetDefaultTenant();

        if (cachedTenant != null)
        {
            logger.LogDebug("Found default tenant in cache: {TenantSlug}", cachedTenant.Slug);

            return cachedTenant;
        }

        // Fallback to database
        Tenant? tenant = await repository.GetDefaultAsync(cancellationToken);

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
        bool isAvailable = await IsSlugAvailableAsync(slug, cancellationToken : cancellationToken);

        if (!isAvailable) { throw new BusinessException($"Tenant slug '{slug}' is already in use"); }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(), Name = name, Slug = slug.ToLowerInvariant(), Description = description, IsActive = true, IsDefault = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
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

        Tenant? tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

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

        Tenant? tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

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

        Tenant? tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

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

        Tenant? tenant = await repository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant == null) { throw new NotFoundException($"Tenant with ID {tenantId} not found"); }

        if (tenant.IsDefault) { throw new BusinessException("Cannot delete the default tenant"); }

        await repository.DeleteAsync(tenantId, cancellationToken);

        logger.LogInformation("Deleted tenant: {TenantId} - {TenantSlug}", tenant.Id, tenant.Slug);

        // Remove from cache
        cacheService.InvalidateTenant(tenantId);
    }

    /// <inheritdoc />
    public async Task<TenantSettings?> GetTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default) { return await tenantSettingsService.GetTenantSettingsAsync(tenantId, cancellationToken); }

    /// <inheritdoc />
    public async Task<TenantSettings> UpdateTenantSettingsAsync(Guid tenantId, TenantSettings settings, CancellationToken cancellationToken = default)
    {
        return await tenantSettingsService.UpdateTenantSettingsAsync(tenantId, settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantDomain>> GetTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await tenantDomainsService.GetTenantDomainsAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TenantDomain> AddTenantDomainAsync(Guid tenantId, string topLevelDomain, string? subdomain = null, bool isMainDomain = false, CancellationToken cancellationToken = default)
    {
        return await tenantDomainsService.CreateTenantDomainAsync(tenantId, topLevelDomain, subdomain, isMainDomain, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TenantDomain?> FindTenantByDomainMatchAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) { return null; }

        logger.LogDebug("Finding tenant by domain match: {Email}", email);

        // Extract domain from email
        int atIndex = email.IndexOf('@');

        if (atIndex == -1) { return null; }

        string domain = email[(atIndex + 1)..].ToLowerInvariant();
        string[ ] parts = domain.Split('.');

        if (parts.Length < 2) { return null; }

        // Try with subdomain first (if more than 2 parts)
        string? subdomain = parts.Length > 2 ? parts[0] : null;
        string topLevelDomain = parts.Length > 2 ? string.Join(".", parts.Skip(1)) : domain;

        logger.LogDebug("Searching for domain match: subdomain={Subdomain}, topLevel={TopLevel}", subdomain, topLevelDomain);

        TenantDomain? tenantDomain = await tenantDomainsService.FindTenantDomainByMatchAsync(topLevelDomain, subdomain, cancellationToken);

        // If not found with subdomain, try without subdomain
        if (tenantDomain == null && subdomain != null) { tenantDomain = await tenantDomainsService.FindTenantDomainByMatchAsync(domain, null, cancellationToken); }

        if (tenantDomain != null) { logger.LogDebug("Found tenant domain: {TenantId}", tenantDomain.TenantId); }
        else { logger.LogDebug("No tenant domain found for: {Domain}", domain); }

        return tenantDomain;
    }

    /// <inheritdoc />
    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) { return false; }

        logger.LogDebug("Checking slug availability: {TenantSlug}", slug);

        bool isAvailable = await repository.IsSlugAvailableAsync(slug, excludeTenantId, cancellationToken);

        logger.LogDebug("Slug {TenantSlug} availability: {IsAvailable}", slug, isAvailable);

        return isAvailable;
    }
}
