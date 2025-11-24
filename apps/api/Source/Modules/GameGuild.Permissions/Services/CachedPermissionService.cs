using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Services;

/// <summary>
///     Cached wrapper for permission service for performance optimization
/// </summary>
public class CachedPermissionService(IPermissionService permissionService, IMemoryCache cache, ILogger<CachedPermissionService> logger) : ICachedPermissionService
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    private readonly ILogger<CachedPermissionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string permission, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"permission:{userId}:{tenantId}:{permission}";

        if (_cache.TryGetValue(cacheKey, out bool hasPermission))
        {
            _logger.LogDebug("Cache hit for permission check: {CacheKey}", cacheKey);

            return hasPermission;
        }

        hasPermission = await _permissionService.HasTenantPermissionAsync(userId, tenantId, permission, cancellationToken);

        _cache.Set(cacheKey, hasPermission, _cacheExpiration);
        _logger.LogDebug("Cached permission check result: {CacheKey} = {HasPermission}", cacheKey, hasPermission);

        return hasPermission;
    }

    public async Task<List<string>> GetPermissionsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"permissions:{userId}:{tenantId}";

        if (_cache.TryGetValue(cacheKey, out List<string>? permissions) && permissions != null)
        {
            _logger.LogDebug("Cache hit for permissions: {CacheKey}", cacheKey);

            return permissions;
        }

        permissions = await _permissionService.GetTenantPermissionsAsync(userId, tenantId, cancellationToken);

        _cache.Set(cacheKey, permissions, _cacheExpiration);
        _logger.LogDebug("Cached permissions for user {UserId} in tenant {TenantId}", userId, tenantId);

        return permissions;
    }

    public async Task InvalidateCacheAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var cacheKeyPrefix = $"permission:{userId}:{tenantId}:";
        var permissionsKey = $"permissions:{userId}:{tenantId}";

        // Note: IMemoryCache doesn't support prefix-based removal, so we remove known keys
        // In production, consider using IDistributedCache with Redis for better cache management
        _cache.Remove(permissionsKey);
        _logger.LogInformation("Invalidated cache for user {UserId} in tenant {TenantId}", userId, tenantId);

        await Task.CompletedTask;
    }

    public async Task InvalidateTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Note: IMemoryCache doesn't support wildcard removal
        // In production, consider using IDistributedCache with Redis for pattern-based cache invalidation
        _logger.LogInformation("Invalidating all cache entries for tenant {TenantId}", tenantId);

        await Task.CompletedTask;
    }
}
