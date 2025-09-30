using System.Security.Claims;
using GameGuild.Core.Domain.Identity;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Permissions.Contexts;

/// <summary>
/// Simplified domain service providing user and tenant-aware permission context
/// Supports three-layer permission architecture (Tenant-wide, Content-Type, Resource-specific)
/// Integrates with ASP.NET Core authentication and authorization
/// </summary>
public class PermissionsContext(IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext, IPermissionService permissionService, ILogger<PermissionsContext> logger) : IPermissionsContext
{
    // === USER AND TENANT CONTEXT ===

    /// <summary>Current user ID from HTTP context</summary>
    public Guid? UserId => GetCurrentUserId();

    /// <summary>Current tenant ID from HTTP context</summary>
    public Guid? TenantId => tenantContext.CurrentTenantId;

    /// <summary>Whether user is authenticated</summary>
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    // === LAYER 1: TENANT-WIDE PERMISSIONS ===

    /// <summary>Check tenant-wide permission for current user</summary>
    public async Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue) { return false; }

        try
        {
            Guid? effectiveTenantId = tenantId ?? TenantId;

            return await permissionService.HasTenantPermissionAsync(UserId.Value, effectiveTenantId, permission);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking tenant permission {Permission} for user {UserId}", permission, UserId);

            return false;
        }
    }

    // === LAYER 2: CONTENT-TYPE PERMISSIONS ===

    /// <summary>Check content-type specific permission for current user</summary>
    public async Task<bool> HasContentTypePermissionAsync(PermissionType permission, string contentType, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue) { return false; }

        try
        {
            Guid? effectiveTenantId = tenantId ?? TenantId;

            return await permissionService.HasContentTypePermissionAsync(UserId.Value, effectiveTenantId, contentType, permission);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking content-type permission {Permission} for content type {ContentType}", permission, contentType);

            return false;
        }
    }

    // === LAYER 3: RESOURCE-SPECIFIC PERMISSIONS ===

    /// <summary>Check resource-specific permission for current user</summary>
    public async Task<bool> HasResourcePermissionAsync(PermissionType permission, string resourceType, Guid resourceId, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue) { return false; }

        try
        {
            // For simplified implementation, resource permissions are handled through content-type permissions
            // Real resource-level permissions would require generic type resolution based on resourceType
            return await HasContentTypePermissionAsync(permission, resourceType, tenantId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking resource permission {Permission} for resource {ResourceType}:{ResourceId}", permission, resourceType, resourceId);

            return false;
        }
    }

    /// <summary>Check if user has any of the specified permissions</summary>
    public async Task<bool> HasAnyTenantPermissionAsync(PermissionType[ ] permissions, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue) { return false; }

        foreach (PermissionType permission in permissions)
        {
            if (await HasTenantPermissionAsync(permission, tenantId)) { return true; }
        }

        return false;
    }

    // === ADMIN UTILITIES ===

    /// <summary>Synchronous check if current user is a system admin (cached or simplified check)</summary>
    public bool IsSystemAdmin
    {
        get
        {
            // For interface compatibility - could be enhanced with caching if needed
            // For now, return false - real checks should use async methods
            return false;
        }
    }

    /// <summary>Synchronous check if current user is a tenant admin (cached or simplified check)</summary>
    public bool IsTenantAdmin
    {
        get
        {
            // For interface compatibility - could be enhanced with caching if needed
            // For now, return false - real checks should use async methods
            return false;
        }
    }

    /// <summary>Check if current user is a tenant admin (has TenantAdmin permission)</summary>
    public async Task<bool> IsTenantAdminAsync(Guid? tenantId = null) { return await HasTenantPermissionAsync(PermissionType.TenantAdmin, tenantId); }

    /// <summary>Check if current user is a system admin (has SystemAdmin permission)</summary>  
    public async Task<bool> IsSystemAdminAsync() { return await HasTenantPermissionAsync(PermissionType.SystemAdmin); }

    // === PRIVATE HELPERS ===

    private Guid? GetCurrentUserId()
    {
        string? userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdClaim, out Guid userId) ? userId : null;
    }
}
