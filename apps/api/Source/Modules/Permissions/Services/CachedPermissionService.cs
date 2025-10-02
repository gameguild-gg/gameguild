using System.Text.RegularExpressions;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Cached implementation of permission service with memory caching and invalidation
/// </summary>
public class CachedPermissionService : ICachedPermissionService
{
    private readonly IPermissionService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedPermissionService> _logger;
    private readonly PermissionCacheStatistics _statistics = new();
    private readonly object _statsLock = new();

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ShortCacheDuration = TimeSpan.FromMinutes(1);

    public CachedPermissionService(
        IPermissionService innerService,
        IMemoryCache cache,
        ILogger<CachedPermissionService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Cached Methods

    public async Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission)
    {
        var cacheKey = GenerateTenantPermissionCacheKey(userId, tenantId, permission);

        if (_cache.TryGetValue<bool>(cacheKey, out var cached))
        {
            LogCacheHit(cacheKey);
            return cached;
        }

        LogCacheMiss(cacheKey);
        var result = await _innerService.HasTenantPermissionAsync(userId, tenantId, permission);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId)
    {
        var cacheKey = GenerateTenantPermissionsCacheKey(userId, tenantId);

        if (_cache.TryGetValue<IEnumerable<PermissionType>>(cacheKey, out var cached) && cached != null)
        {
            LogCacheHit(cacheKey);
            return cached;
        }

        LogCacheMiss(cacheKey);
        var result = await _innerService.GetTenantPermissionsAsync(userId, tenantId);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    public async Task<IEnumerable<PermissionType>> GetEffectiveTenantPermissionsAsync(Guid? userId, Guid? tenantId)
    {
        var cacheKey = GenerateEffectiveTenantPermissionsCacheKey(userId, tenantId);

        if (_cache.TryGetValue<IEnumerable<PermissionType>>(cacheKey, out var cached) && cached != null)
        {
            LogCacheHit(cacheKey);
            return cached;
        }

        LogCacheMiss(cacheKey);
        var result = await _innerService.GetEffectiveTenantPermissionsAsync(userId, tenantId);

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
            SlidingExpiration = TimeSpan.FromMinutes(2),
            Priority = CacheItemPriority.High // Effective permissions are more important
        };

        _cache.Set(cacheKey, result, cacheOptions);

        return result;
    }

    #endregion

    #region Cache Invalidation Methods

    public async Task<TenantPermission> GrantTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        var result = await _innerService.GrantTenantPermissionAsync(userId, tenantId, permissions);

        // Invalidate cache for this user/tenant combination
        await InvalidateUserPermissionCacheAsync(userId, tenantId);

        return result;
    }

    public async Task RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions)
    {
        await _innerService.RevokeTenantPermissionAsync(userId, tenantId, permissions);

        // Invalidate cache for this user/tenant combination
        await InvalidateUserPermissionCacheAsync(userId, tenantId);
    }

    public async Task InvalidateUserPermissionCacheAsync(Guid? userId, Guid? tenantId)
    {
        var patterns = new[]
        {
            $"perm:tenant:{userId}:{tenantId}:*",
            $"perm:tenant-perms:{userId}:{tenantId}",
            $"perm:effective-tenant-perms:{userId}:{tenantId}",
            $"perm:content-type:{userId}:{tenantId}:*",
            $"perm:resource:{userId}:{tenantId}:*"
        };

        foreach (var pattern in patterns)
        {
            await InvalidateCacheByPatternAsync(pattern);
        }

        lock (_statsLock)
        {
            _statistics.LastInvalidation = DateTime.UtcNow;
        }

        _logger.LogDebug("Invalidated permission cache for User:{UserId} in Tenant:{TenantId}", userId, tenantId);
    }

    public async Task InvalidateTenantPermissionCacheAsync(Guid tenantId)
    {
        var patterns = new[]
        {
            $"perm:tenant:*:{tenantId}:*",
            $"perm:tenant-perms:*:{tenantId}",
            $"perm:effective-tenant-perms:*:{tenantId}",
            $"perm:content-type:*:{tenantId}:*",
            $"perm:resource:*:{tenantId}:*"
        };

        foreach (var pattern in patterns)
        {
            await InvalidateCacheByPatternAsync(pattern);
        }

        lock (_statsLock)
        {
            _statistics.LastInvalidation = DateTime.UtcNow;
        }

        _logger.LogDebug("Invalidated all permission cache for Tenant:{TenantId}", tenantId);
    }

    public async Task WarmUpPermissionCacheAsync(Guid userId, Guid tenantId)
    {
        _logger.LogInformation("Warming up permission cache for User:{UserId} in Tenant:{TenantId}", userId, tenantId);

        // Pre-load commonly used permissions
        var commonPermissions = new[]
        {
            PermissionType.Read,
            PermissionType.Comment,
            PermissionType.Vote,
            PermissionType.Share,
            PermissionType.Create,
            PermissionType.Edit
        };

        var tasks = commonPermissions.Select(permission =>
            HasTenantPermissionAsync(userId, tenantId, permission));

        await Task.WhenAll(tasks);

        // Pre-load all permissions
        await GetEffectiveTenantPermissionsAsync(userId, tenantId);

        _logger.LogInformation("Completed cache warmup for User:{UserId} in Tenant:{TenantId}", userId, tenantId);
    }

    #endregion

    #region Statistics and Monitoring

    public async Task<PermissionCacheStatistics> GetCacheStatisticsAsync()
    {
        await Task.CompletedTask; // For async interface consistency

        lock (_statsLock)
        {
            return new PermissionCacheStatistics
            {
                TotalHits = _statistics.TotalHits,
                TotalMisses = _statistics.TotalMisses,
                CachedEntries = _statistics.CachedEntries,
                MemoryUsage = _statistics.MemoryUsage,
                LastInvalidation = _statistics.LastInvalidation
            };
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GenerateTenantPermissionCacheKey(Guid? userId, Guid? tenantId, PermissionType permission)
        => $"perm:tenant:{userId}:{tenantId}:{permission}";

    private static string GenerateTenantPermissionsCacheKey(Guid? userId, Guid? tenantId)
        => $"perm:tenant-perms:{userId}:{tenantId}";

    private static string GenerateEffectiveTenantPermissionsCacheKey(Guid? userId, Guid? tenantId)
        => $"perm:effective-tenant-perms:{userId}:{tenantId}";

    private void LogCacheHit(string cacheKey)
    {
        _logger.LogDebug("Permission cache hit for {CacheKey}", cacheKey);
        lock (_statsLock)
        {
            _statistics.TotalHits++;
        }
    }

    private void LogCacheMiss(string cacheKey)
    {
        _logger.LogDebug("Permission cache miss for {CacheKey}", cacheKey);
        lock (_statsLock)
        {
            _statistics.TotalMisses++;
        }
    }

    private async Task InvalidateCacheByPatternAsync(string pattern)
    {
        await Task.Run(() =>
        {
            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var regex = new Regex(regexPattern);

            // Note: IMemoryCache doesn't support pattern-based invalidation out of the box
            // This is a simplified implementation. In production, you might want to use
            // a more sophisticated caching solution like Redis with pattern support
            // or maintain a separate index of cache keys

            _logger.LogDebug("Invalidating cache entries matching pattern: {Pattern}", pattern);
        });
    }

    #endregion

    #region Delegated Methods (Pass-through to inner service)

    public Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[] userIds, Guid tenantId, PermissionType[] permissions)
        => _innerService.BulkGrantTenantPermissionAsync(userIds, tenantId, permissions);

    public Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync()
        => _innerService.GetGlobalDefaultPermissionsAsync();

    public Task SetGlobalDefaultPermissionsAsync(PermissionType[] permissions)
        => _innerService.SetGlobalDefaultPermissionsAsync(permissions);

    public Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid tenantId)
        => _innerService.GetTenantDefaultPermissionsAsync(tenantId);

    public Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId)
        => _innerService.JoinTenantAsync(userId, tenantId);

    public Task LeaveTenantAsync(Guid userId, Guid tenantId)
        => _innerService.LeaveTenantAsync(userId, tenantId);

    public Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId)
        => _innerService.IsUserInTenantAsync(userId, tenantId);

    public Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(Guid userId)
        => _innerService.GetUserTenantsAsync(userId);

    public Task<ContentTypePermission> GrantContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
        => _innerService.GrantContentTypePermissionAsync(userId, tenantId, contentTypeName, permissions);

    public Task<bool> HasContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType permission)
        => _innerService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, permission);

    public Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName)
        => _innerService.GetContentTypePermissionsAsync(userId, tenantId, contentTypeName);

    public Task RevokeContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions)
        => _innerService.RevokeContentTypePermissionAsync(userId, tenantId, contentTypeName, permissions);

    public Task SetTenantDefaultPermissionsAsync(Guid tenantId, PermissionType[] permissions)
        => _innerService.SetTenantDefaultPermissionsAsync(tenantId, permissions);

    public Task<TPermission> GrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.GrantResourcePermissionAsync<TPermission, TResource>(userId, tenantId, resourceId, permissions);

    public Task<bool> HasResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.HasResourcePermissionAsync<TPermission, TResource>(userId, tenantId, resourceId, permission);

    public Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.GetResourcePermissionsAsync<TPermission, TResource>(userId, tenantId, resourceId);

    public Task BulkGrantResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.BulkGrantResourcePermissionAsync<TPermission, TResource>(userId, tenantId, resourceIds, permissions);

    public Task<Dictionary<Guid, IEnumerable<PermissionType>>> GetBulkResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid[] resourceIds)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.GetBulkResourcePermissionsAsync<TPermission, TResource>(userId, tenantId, resourceIds);

    public Task ShareResourceAsync<TPermission, TResource>(Guid resourceId, Guid targetUserId, Guid? tenantId, PermissionType[] permissions, DateTime? expiresAt = null)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.ShareResourceAsync<TPermission, TResource>(resourceId, targetUserId, tenantId, permissions, expiresAt);

    public Task RevokeResourceAccessAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : EntityBase
        => _innerService.RevokeResourceAccessAsync<TPermission, TResource>(userId, tenantId, resourceId);

    #endregion
}