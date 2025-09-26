using System.Collections.Concurrent;
using GameGuild.Database;

namespace GameGuild.Modules.Tenants;

/// <summary>
///     Implementation of tenant caching service for high performance access to tenant data
///     Includes cache refresh functionality (merged from TenantCacheRefreshService)
/// </summary>
public class TenantCacheService : ITenantCacheService
{
    private readonly ConcurrentDictionary<string, TenantDomain> _domainMatchCache = new ConcurrentDictionary<string, TenantDomain>();

    private readonly object _initializationLock = new object();

    private readonly ILogger<TenantCacheService> _logger;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    // Thread-safe caches
    private readonly ConcurrentDictionary<Guid, Tenant> _tenantCache = new ConcurrentDictionary<Guid, Tenant>();

    private readonly ConcurrentDictionary<Guid, List<TenantDomain>> _tenantDomainsCache = new ConcurrentDictionary<Guid, List<TenantDomain>>();

    private readonly ConcurrentDictionary<Guid, TenantSettings> _tenantSettingsCache = new ConcurrentDictionary<Guid, TenantSettings>();

    private readonly ConcurrentDictionary<string, Tenant> _tenantSlugCache = new ConcurrentDictionary<string, Tenant>();

    private volatile bool _isInitialized;

    private DateTime _lastRefreshTime = DateTime.MinValue;

    public TenantCacheService(IServiceScopeFactory serviceScopeFactory, ILogger<TenantCacheService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task InitializeCacheAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        lock (_initializationLock)
        {
            if (_isInitialized) return;
        }

        _logger.LogInformation("Initializing tenant cache...");

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await LoadTenantsAsync(context, cancellationToken);
            await LoadTenantSettingsAsync(context, cancellationToken);
            await LoadTenantDomainsAsync(context, cancellationToken);

            _isInitialized = true;
            _lastRefreshTime = DateTime.UtcNow;

            TenantCacheStatistics stats = GetCacheStatistics();

            _logger.LogInformation(
                "Tenant cache initialized successfully - {TenantCount} tenants, {SettingsCount} settings, {DomainsCount} domains",
                stats.TenantCount,
                stats.TenantSettingsCount,
                stats.TenantDomainsCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tenant cache");

            throw;
        }
    }

    public Tenant? GetTenantById(Guid tenantId)
    {
        EnsureInitialized();
        _tenantCache.TryGetValue(tenantId, out Tenant? tenant);

        return tenant;
    }

    public Tenant? GetTenantBySlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        EnsureInitialized();
        _tenantSlugCache.TryGetValue(slug.ToLowerInvariant(), out Tenant? tenant);

        return tenant;
    }

    public Tenant? GetDefaultTenant()
    {
        EnsureInitialized();

        return _tenantCache.Values.FirstOrDefault(t => t.IsDefault);
    }

    public TenantSettings? GetTenantSettings(Guid tenantId)
    {
        EnsureInitialized();
        _tenantSettingsCache.TryGetValue(tenantId, out TenantSettings? settings);

        return settings;
    }

    public IReadOnlyList<TenantDomain> GetTenantDomains(Guid tenantId)
    {
        EnsureInitialized();
        _tenantDomainsCache.TryGetValue(tenantId, out var domains);

        return domains?.AsReadOnly() ?? new List<TenantDomain>().AsReadOnly();
    }

    public TenantDomain? GetMainTenantDomain(Guid tenantId)
    {
        var domains = GetTenantDomains(tenantId);

        return domains.FirstOrDefault(d => d.IsMainDomain);
    }

    public TenantDomain? FindTenantByDomainMatch(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        EnsureInitialized();

        string domain = ExtractDomainFromEmail(email);

        if (string.IsNullOrEmpty(domain)) return null;

        _domainMatchCache.TryGetValue(domain, out TenantDomain? tenantDomain);

        return tenantDomain;
    }

    public IReadOnlyList<Tenant> GetActiveTenants()
    {
        EnsureInitialized();

        return _tenantCache.Values.Where(t => t.IsActive).ToList().AsReadOnly();
    }

    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing tenant cache...");

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await LoadTenantsAsync(context, cancellationToken);
            await LoadTenantSettingsAsync(context, cancellationToken);
            await LoadTenantDomainsAsync(context, cancellationToken);

            _lastRefreshTime = DateTime.UtcNow;

            TenantCacheStatistics stats = GetCacheStatistics();
            _logger.LogInformation("Tenant cache refreshed successfully - {TenantCount} tenants, {SettingsCount} settings, {DomainsCount} domains", stats.TenantCount, stats.TenantSettingsCount, stats.TenantDomainsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tenant cache");

            throw;
        }
    }

    public void InvalidateTenant(Guid tenantId)
    {
        _logger.LogDebug("Invalidating tenant {TenantId} from cache", tenantId);

        if (_tenantCache.TryRemove(tenantId, out Tenant? tenant))
        {
            // Also remove from slug cache
            if (!string.IsNullOrEmpty(tenant.Slug)) { _tenantSlugCache.TryRemove(tenant.Slug.ToLowerInvariant(), out _); }
        }

        // Remove associated data
        _tenantSettingsCache.TryRemove(tenantId, out _);
        _tenantDomainsCache.TryRemove(tenantId, out _);
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing all tenant cache data");

        _tenantCache.Clear();
        _tenantSlugCache.Clear();
        _tenantSettingsCache.Clear();
        _tenantDomainsCache.Clear();
        _domainMatchCache.Clear();

        _isInitialized = false;
        _lastRefreshTime = DateTime.MinValue;
    }

    public TenantCacheStatistics GetCacheStatistics()
    {
        return new TenantCacheStatistics
        {
            TenantCount = _tenantCache.Count,
            TenantSettingsCount = _tenantSettingsCache.Count,
            TenantDomainsCount = _tenantDomainsCache.Values.Sum(domains => domains.Count),
            LastRefreshTime = _lastRefreshTime,
            IsInitialized = _isInitialized
        };
    }

    public async Task RefreshTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing tenant {TenantId} in cache", tenantId);

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant != null)
            {
                _tenantCache[tenantId] = tenant;
                _tenantSlugCache[tenant.Slug.ToLowerInvariant()] = tenant;
                _logger.LogDebug("Tenant {TenantId} refreshed in cache", tenantId);
            }
            else
            {
                InvalidateTenant(tenantId);
                _logger.LogDebug("Tenant {TenantId} not found, removed from cache", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tenant {TenantId} in cache", tenantId);

            throw;
        }
    }

    public async Task RefreshTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing tenant settings for {TenantId} in cache", tenantId);

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            TenantSettings? settings = await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

            if (settings != null)
            {
                _tenantSettingsCache[tenantId] = settings;
                _logger.LogDebug("Tenant settings for {TenantId} refreshed in cache", tenantId);
            }
            else
            {
                _tenantSettingsCache.TryRemove(tenantId, out _);
                _logger.LogDebug("Tenant settings for {TenantId} not found, removed from cache", tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tenant settings for {TenantId} in cache", tenantId);

            throw;
        }
    }

    public async Task RefreshTenantDomainsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing tenant domains for {TenantId} in cache", tenantId);

        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var domains = await context.TenantDomains.Where(d => d.TenantId == tenantId).ToListAsync(cancellationToken);

            _tenantDomainsCache[tenantId] = domains;

            // Update domain match cache
            foreach (TenantDomain domain in domains)
            {
                string fullDomain = string.IsNullOrEmpty(domain.Subdomain) ? domain.TopLevelDomain : $"{domain.Subdomain}.{domain.TopLevelDomain}";
                _domainMatchCache[fullDomain] = domain;
            }

            _logger.LogDebug("Tenant domains for {TenantId} refreshed in cache ({DomainCount} domains)", tenantId, domains.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh tenant domains for {TenantId} in cache", tenantId);

            throw;
        }
    }

    public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing full tenant cache refresh");
        await RefreshCacheAsync(cancellationToken);
    }

    private async Task LoadTenantsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading tenants into cache");

        var tenants = await context.Tenants.Where(t => !t.IsDeleted).ToListAsync(cancellationToken);

        _tenantCache.Clear();
        _tenantSlugCache.Clear();

        foreach (Tenant tenant in tenants)
        {
            _tenantCache[tenant.Id] = tenant;
            _tenantSlugCache[tenant.Slug.ToLowerInvariant()] = tenant;
        }

        _logger.LogDebug("Loaded {TenantCount} tenants into cache", tenants.Count);
    }

    private async Task LoadTenantSettingsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading tenant settings into cache");

        var settings = await context.TenantSettings.ToListAsync(cancellationToken);

        _tenantSettingsCache.Clear();

        foreach (TenantSettings setting in settings)
        {
            if (setting.TenantId.HasValue) { _tenantSettingsCache[setting.TenantId.Value] = setting; }
        }

        _logger.LogDebug("Loaded {SettingsCount} tenant settings into cache", settings.Count);
    }

    private async Task LoadTenantDomainsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading tenant domains into cache");

        var domains = await context.TenantDomains.ToListAsync(cancellationToken);

        _tenantDomainsCache.Clear();
        _domainMatchCache.Clear();

        var groupedDomains = domains.GroupBy(d => d.TenantId);

        foreach (var group in groupedDomains)
        {
            _tenantDomainsCache[group.Key] = group.ToList();

            foreach (TenantDomain domain in group)
            {
                string fullDomain = string.IsNullOrEmpty(domain.Subdomain) ? domain.TopLevelDomain : $"{domain.Subdomain}.{domain.TopLevelDomain}";
                _domainMatchCache[fullDomain] = domain;
            }
        }

        _logger.LogDebug("Loaded {DomainCount} tenant domains into cache for {TenantCount} tenants", domains.Count, groupedDomains.Count());
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized) { throw new InvalidOperationException("Tenant cache has not been initialized. Call InitializeCacheAsync first."); }
    }

    private static string ExtractDomainFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;

        int atIndex = email.LastIndexOf('@');

        return atIndex > 0 && atIndex < email.Length - 1 ? email.Substring(atIndex + 1).ToLowerInvariant() : string.Empty;
    }
}
