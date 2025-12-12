using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Infrastructure.Attributes;

/// <summary>
///     Requires a tenant-level permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireTenantPermissionAttribute(string permission) : RequirePermissionAttribute(permission);

/// <summary>
///     Requires a content-type level permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireContentTypePermissionAttribute(string permission) : RequirePermissionAttribute(permission, PermissionLayer.ContentType);

/// <summary>
///     Requires a resource-level permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireResourcePermissionAttribute : RequirePermissionAttribute
{
    public RequireResourcePermissionAttribute(string permission, string resourceType, string resourceIdParameter = "id") : base(permission, PermissionLayer.Resource)
    {
        ResourceType = resourceType;
        ResourceIdParameter = resourceIdParameter;
    }
}

/// <summary>
///     Requires system administrator role
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSystemAdminAttribute() : RequirePermissionAttribute("SystemAdmin");

/// <summary>
///     Requires tenant administrator role
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAdminAttribute() : RequirePermissionAttribute("TenantAdmin");

/// <summary>
///     Requires user to be the owner of the resource OR have the specified permission
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireOwnerOrPermissionAttribute : RequirePermissionAttribute
{
    public RequireOwnerOrPermissionAttribute(string permission, string resourceType, string resourceIdParameter = "id") : base(permission, PermissionLayer.Resource)
    {
        ResourceType = resourceType;
        ResourceIdParameter = resourceIdParameter;
        RequireOwnership = true;
    }
}

/// <summary>
///     Requires a custom policy to be satisfied
///     Policies are defined and registered in the DI container
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePolicyAttribute(string policyName) : RequirePermissionAttribute(policyName, PermissionLayer.Auto)
{
    public string PolicyName { get; } = policyName;
}

/// <summary>
///     Dynamic Authorization based on Claims (DAC)
///     Combines Game Guild's DAC pattern with GameGuild's permission system
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class DACAuthorizationAttribute(string permission) : RequirePermissionAttribute(permission, PermissionLayer.Auto)
{
    public string? TenantClaimType { get; set; } = "tenant_id";

    public string? ContentTypeClaimType { get; set; } = "content_type";

    public string? ResourceClaimType { get; set; } = "resource_id";
}
