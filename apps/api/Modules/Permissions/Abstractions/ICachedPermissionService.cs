using GameGuild.Modules.Permissions.Models;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Cached permission service interface that extends IPermissionService with caching capabilities
/// </summary>
public interface ICachedPermissionService : IPermissionService
{
    /// <summary>
    /// Invalidate all cached permissions for a user in a tenant
    /// </summary>
    Task InvalidateUserPermissionCacheAsync(Guid? userId, Guid? tenantId);

    /// <summary>
    /// Invalidate all cached permissions for a tenant
    /// </summary>
    Task InvalidateTenantPermissionCacheAsync(Guid tenantId);

    /// <summary>
    /// Warm up permission cache for a user in a tenant
    /// </summary>
    Task WarmUpPermissionCacheAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<PermissionCacheStatistics> GetCacheStatisticsAsync();
}