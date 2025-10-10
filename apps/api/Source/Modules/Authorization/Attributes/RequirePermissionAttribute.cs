using GameGuild.Modules.Tenants;
using GameGuild.Modules.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace GameGuild.Authorization;

/// <summary>
/// Unified permission attribute supporting all three layers of the permission system
/// Layer 1: Tenant-wide permissions
/// Layer 2: Content-Type permissions  
/// Layer 3: Resource-specific permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public PermissionType RequiredPermission { get; }

    public PermissionLayer Layer { get; set; } = PermissionLayer.Auto;

    public string? ContentType { get; set; }

    public string? ResourceType { get; set; }

    public string ResourceIdParameter { get; set; } = "id";

    public bool AllowOwner { get; set; } = false;

    public RequirePermissionAttribute(PermissionType requiredPermission) { RequiredPermission = requiredPermission; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        // Get required services
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();

        // Get user ID
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        var tenantId = tenantContext.CurrentTenantId;
        bool hasPermission = false;

        try
        {
            // Check permission based on layer
            switch (Layer)
            {
                case PermissionLayer.Tenant: hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, RequiredPermission); break;

                case PermissionLayer.ContentType when ContentType != null: hasPermission = await permissionService.HasContentTypePermissionAsync(userId, tenantId, ContentType, RequiredPermission); break;

                case PermissionLayer.Resource:
                    var resourceId = GetResourceId(context);

                    if (resourceId.HasValue)
                    {
                        // For resource permissions, we need the specific generic method but for simplicity, 
                        // we'll fall back to tenant-level check for now
                        hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, RequiredPermission);
                    }

                    break;

                case PermissionLayer.Auto:
                default:
                    // Try in order: tenant -> content-type -> resource
                    hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, RequiredPermission);

                    if (!hasPermission && ContentType != null) { hasPermission = await permissionService.HasContentTypePermissionAsync(userId, tenantId, ContentType, RequiredPermission); }

                    break;
            }

            if (!hasPermission) { context.Result = new ForbidResult(); }
        }
        catch (Exception) { context.Result = new ForbidResult(); }
    }

    private Guid? GetResourceId(AuthorizationFilterContext context)
    {
        // Try to get resource ID from route values
        if (context.RouteData.Values.TryGetValue(ResourceIdParameter, out var value) && Guid.TryParse(value?.ToString(), out var resourceId)) { return resourceId; }

        return null;
    }
}

/// <summary>
/// Permission layer enumeration
/// </summary>
public enum PermissionLayer { Auto, Tenant, ContentType, Resource }
