namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified permission context that wraps IPermissionService with current user/tenant context
///     Provides convenient permission checking without repeatedly passing userId/tenantId
/// </summary>
public class PermissionsContext(IUserContext userContext, ITenantContext tenantContext, IPermissionService permissionService) : IPermissionsContext
{
    public Guid? UserId => userContext.UserId;

    public Guid? TenantId => tenantContext.TenantId;

    public bool IsAuthenticated => userContext.IsAuthenticated;

    public bool IsSystemAdmin => userContext.IsInRole("SystemAdmin") || userContext.IsInRole("Admin");

    public bool IsTenantAdmin => userContext.IsInRole("TenantAdmin") || IsSystemAdmin;

    public async Task<bool> HasTenantPermissionAsync(string permission, Guid? tenantId = null)
    {
        if (!UserId.HasValue) return false;

        var effectiveTenantId = tenantId ?? TenantId;

        if (!effectiveTenantId.HasValue) return false;

        // System admins have all permissions
        if (IsSystemAdmin) return true;

        return await permissionService.HasTenantPermissionAsync(UserId.Value, effectiveTenantId.Value, permission);
    }

    public async Task<bool> HasResourcePermissionAsync(string resourceType, Guid resourceId, string permission)
    {
        if (!UserId.HasValue) return false;
        if (!TenantId.HasValue) return false;

        // System admins have all permissions
        if (IsSystemAdmin) return true;

        // For now, delegate to tenant permission with resource-specific permission name
        // This can be enhanced with a dedicated resource permission service
        var resourcePermission = $"{resourceType}.{resourceId}.{permission}";

        return await permissionService.HasTenantPermissionAsync(UserId.Value, TenantId.Value, resourcePermission);
    }

    public async Task<List<string>> GetEffectivePermissionsAsync()
    {
        if (!UserId.HasValue) return new List<string>();

        return await permissionService.GetEffectivePermissionsAsync(UserId.Value, TenantId);
    }

    public bool IsOwner(Guid? resourceOwnerId)
    {
        if (!UserId.HasValue || !resourceOwnerId.HasValue) return false;

        return UserId.Value == resourceOwnerId.Value;
    }
}
