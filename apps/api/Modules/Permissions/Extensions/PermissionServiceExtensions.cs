using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Extensions;

/// <summary>
/// Extension methods for permission services
/// </summary>
public static class PermissionServiceExtensions
{
    /// <summary>
    /// Check if user has any of the specified permissions
    /// </summary>
    public static async Task<bool> HasAnyPermissionAsync(
        this IPermissionService permissionService,
        Guid? userId,
        Guid? tenantId,
        params PermissionType[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (await permissionService.HasTenantPermissionAsync(userId, tenantId, permission))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if user has all of the specified permissions
    /// </summary>
    public static async Task<bool> HasAllPermissionsAsync(
        this IPermissionService permissionService,
        Guid? userId,
        Guid? tenantId,
        params PermissionType[] permissions)
    {
        foreach (var permission in permissions)
        {
            if (!await permissionService.HasTenantPermissionAsync(userId, tenantId, permission))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Get permissions summary for a user in a tenant
    /// </summary>
    public static async Task<PermissionSummary> GetPermissionSummaryAsync(
        this IPermissionService permissionService,
        Guid userId,
        Guid tenantId)
    {
        var summary = new PermissionSummary
        {
            UserId = userId,
            TenantId = tenantId,
            CheckedAt = DateTime.UtcNow,
            Permissions = new Dictionary<PermissionType, bool>()
        };

        // Check all permission types
        var allPermissionTypes = Enum.GetValues<PermissionType>();
        foreach (var permissionType in allPermissionTypes)
        {
            summary.Permissions[permissionType] =
                await permissionService.HasTenantPermissionAsync(userId, tenantId, permissionType);
        }

        return summary;
    }
}

/// <summary>
/// Extension methods for cached permission services
/// </summary>
public static class CachedPermissionServiceExtensions
{
    /// <summary>
    /// Batch warm up cache for multiple users
    /// </summary>
    public static async Task WarmUpPermissionCacheAsync(
        this ICachedPermissionService cachedService,
        IEnumerable<(Guid UserId, Guid TenantId)> userTenantPairs)
    {
        var tasks = userTenantPairs.Select(pair =>
            cachedService.WarmUpPermissionCacheAsync(pair.UserId, pair.TenantId));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Batch invalidate cache for multiple users
    /// </summary>
    public static async Task InvalidateUserPermissionCacheAsync(
        this ICachedPermissionService cachedService,
        IEnumerable<(Guid? UserId, Guid? TenantId)> userTenantPairs)
    {
        var tasks = userTenantPairs.Select(pair =>
            cachedService.InvalidateUserPermissionCacheAsync(pair.UserId, pair.TenantId));

        await Task.WhenAll(tasks);
    }
}

/// <summary>
/// Summary of user permissions in a tenant
/// </summary>
public class PermissionSummary
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CheckedAt { get; set; }
    public Dictionary<PermissionType, bool> Permissions { get; set; } = new();

    public int GrantedCount => Permissions.Values.Count(p => p);
    public int TotalCount => Permissions.Count;
    public double PermissionRatio => TotalCount > 0 ? (double)GrantedCount / TotalCount : 0;
}