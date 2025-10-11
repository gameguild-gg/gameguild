namespace GameGuild.Authorization;
using GameGuild.Modules.Tenants;
using Modules.Resources;

/// <summary>
/// Convenience attributes for common permission scenarios
/// These inherit from RequirePermissionAttribute to provide simple, focused usage
/// </summary>
/// <summary>Tenant-level permission check</summary>
public class RequireTenantPermissionAttribute : RequirePermissionAttribute
{
    public RequireTenantPermissionAttribute(PermissionType permission) : base(permission) { Layer = PermissionLayer.Tenant; }
}

/// <summary>Content-type permission check</summary>
public class RequireContentTypePermissionAttribute : RequirePermissionAttribute
{
    public RequireContentTypePermissionAttribute(PermissionType permission, string contentType) : base(permission)
    {
        Layer = PermissionLayer.ContentType;
        ContentType = contentType;
    }
}

/// <summary>Resource-specific permission check</summary>
public class RequireResourcePermissionAttribute : RequirePermissionAttribute
{
    public RequireResourcePermissionAttribute(PermissionType permission, string resourceType, string resourceIdParameter = "id") : base(permission)
    {
        Layer = PermissionLayer.Resource;
        ResourceType = resourceType;
        ResourceIdParameter = resourceIdParameter;
    }
}

/// <summary>System administrator access required (MAC)</summary>
public class RequireSystemAdminAttribute : RequirePermissionAttribute
{
    public RequireSystemAdminAttribute() : base(PermissionType.SystemAdmin) { Layer = PermissionLayer.Tenant; }
}

/// <summary>Tenant administrator access required (MAC)</summary>
public class RequireTenantAdminAttribute : RequirePermissionAttribute
{
    public RequireTenantAdminAttribute() : base(PermissionType.TenantAdmin) { Layer = PermissionLayer.Tenant; }
}

/// <summary>Owner-only access with optional permission fallback</summary>
public class RequireOwnerOrPermissionAttribute : RequirePermissionAttribute
{
    public RequireOwnerOrPermissionAttribute(PermissionType fallbackPermission, string resourceType, string resourceIdParameter = "id") : base(fallbackPermission)
    {
        Layer = PermissionLayer.Resource;
        ResourceType = resourceType;
        ResourceIdParameter = resourceIdParameter;
        AllowOwner = true; // Use the correct property name
    }
}

/// <summary>Policy-based ABAC permission check</summary>
public class RequirePolicyAttribute : RequirePermissionAttribute
{
    public RequirePolicyAttribute(PermissionType fallbackPermission = PermissionType.Read) : base(fallbackPermission)
    {
        Layer = PermissionLayer.Auto;
        // Policy evaluation handled in the permission service
    }
}
