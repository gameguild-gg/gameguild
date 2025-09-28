using GameGuild.Core.Domain.Identity;
using GameGuild.Core.Domain.Permissions;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;

namespace GameGuild.Authorization.Identity;

/// <summary> Implementation of permissions context for the current request Provides centralized permission checking and authorization services </summary>
public class PermissionsContext(
    IUserContext userContext,
    ITenantContext tenantContext,
    IPermissionService permissionService,
    IPermissionResolver permissionResolver,
    IModulePermissionService modulePermissionService,
    ILogger<PermissionsContext> logger
) : IPermissionsContext
{
    private readonly IModulePermissionService _modulePermissionService = modulePermissionService;

    // === CONTEXT PROPERTIES ===

    public Guid? UserId { get => userContext.UserId; }

    public Guid? TenantId { get => tenantContext.CurrentTenantId; }

    public bool IsAuthenticated { get => userContext.IsAuthenticated; }

    public bool IsSystemAdmin { get => userContext.IsInRole("SystemAdmin") || userContext.IsInRole("SuperAdmin"); }

    public bool IsTenantAdmin { get => userContext.IsInRole("TenantAdmin") || userContext.IsInRole("Admin"); }

    // === BASIC PERMISSION CHECKS ===

    public async Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            logger.LogDebug("Permission check failed: User not authenticated");

            return false;
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try { return await permissionService.HasTenantPermissionAsync(UserId.Value, effectiveTenantId, permission); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking tenant permission {Permission} for user {UserId} in tenant {TenantId}", permission, UserId, effectiveTenantId);

            return false;
        }
    }

    public async Task<bool> HasContentTypePermissionAsync(PermissionType permission, string contentType, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            logger.LogDebug("Permission check failed: User not authenticated");

            return false;
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try { return await permissionService.HasContentTypePermissionAsync(UserId.Value, effectiveTenantId, contentType, permission); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking content type permission {Permission} for user {UserId} in tenant {TenantId} for content type {ContentType}", permission, UserId, effectiveTenantId, contentType);

            return false;
        }
    }

    public async Task<bool> HasResourcePermissionAsync(PermissionType permission, string resourceType, Guid resourceId, Guid? tenantId = null)
    {
        if (!IsAuthenticated || !UserId.HasValue)
        {
            logger.LogDebug("Permission check failed: User not authenticated");

            return false;
        }

        var effectiveTenantId = tenantId ?? TenantId;

        try
        {
            // Use IPermissionResolver to handle resource type mapping
            var result = await permissionResolver.ResolvePermissionAsync<EntityBase>(UserId.Value, effectiveTenantId, permission, resourceId, resourceType);

            return result.IsGranted;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error checking resource permission {Permission} for user {UserId} in tenant {TenantId} for resource {ResourceType}:{ResourceId}",
                permission,
                UserId,
                effectiveTenantId,
                resourceType,
                resourceId
            );

            return false;
        }
    }

    public async Task<bool> HasAnyTenantPermissionAsync(PermissionType[ ] permissions, Guid? tenantId = null)
    {
        if (!(permissions?.Length > 0)) return false;

        foreach (var permission in permissions)
        {
            if (await HasTenantPermissionAsync(permission, tenantId)) { return true; }
        }

        return false;
    }
}
