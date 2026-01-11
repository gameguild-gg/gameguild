using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GameGuild.Authorization;

/// <summary>
///     Authorization filter that processes resource permission attributes on controller actions.
///     This filter intercepts requests and validates permissions before the action executes.
/// </summary>
public class ResourcePermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ILogger<ResourcePermissionAuthorizationFilter> _logger;

    public ResourcePermissionAuthorizationFilter(ILogger<ResourcePermissionAuthorizationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null) return;

        // Get action descriptor for attribute discovery
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (actionDescriptor == null) return;

        var permissionsContext = context.HttpContext.RequestServices.GetService<IPermissionsContext>();
        if (permissionsContext == null)
        {
            _logger.LogWarning("IPermissionsContext not available - skipping authorization check");
            return;
        }

        // Check if user is authenticated
        if (!permissionsContext.IsAuthenticated)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        // System admin bypass
        if (permissionsContext.IsSystemAdmin)
        {
            return;
        }

        // Get all permission attributes from action and controller
        var attributes = GetPermissionAttributes(actionDescriptor);

        foreach (var attr in attributes)
        {
            var authorized = await CheckPermissionAsync(attr, context.HttpContext, permissionsContext);
            if (!authorized)
            {
                _logger.LogWarning(
                    "Authorization failed for user {UserId} on {Controller}.{Action}",
                    permissionsContext.UserId,
                    actionDescriptor.ControllerName,
                    actionDescriptor.ActionName);
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
                return;
            }
        }
    }

    private static IEnumerable<Attribute> GetPermissionAttributes(ControllerActionDescriptor actionDescriptor)
    {
        // Get attributes from action method
        var methodAttributes = actionDescriptor.MethodInfo
            .GetCustomAttributes(true)
            .OfType<Attribute>()
            .Where(IsPermissionAttribute);

        // Get attributes from controller class
        var controllerAttributes = actionDescriptor.ControllerTypeInfo
            .GetCustomAttributes(true)
            .OfType<Attribute>()
            .Where(IsPermissionAttribute);

        return methodAttributes.Concat(controllerAttributes);
    }

    private static bool IsPermissionAttribute(Attribute attr)
    {
        return attr is IResourcePermissionMarker
            || attr is IContentTypePermissionMarker
            || attr is RequireTenantPermissionAttribute
            || attr is RequireTenantPermission
            || attr is RequiresPermissionAttribute
            || attr is RequirePermissionAttribute;
    }

    private async Task<bool> CheckPermissionAsync(
        Attribute attr,
        HttpContext httpContext,
        IPermissionsContext permissionsContext)
    {
        // Handle resource permission attributes
        if (attr is IResourcePermissionMarker resourceAttr)
        {
            return await CheckResourcePermissionAsync(resourceAttr, httpContext, permissionsContext);
        }

        // Handle content-type permission attributes
        if (attr is IContentTypePermissionMarker contentTypeAttr)
        {
            return await CheckContentTypePermissionAsync(contentTypeAttr, permissionsContext);
        }

        // Handle tenant permission attributes
        if (attr is RequireTenantPermissionAttribute tenantAttr)
        {
            return await CheckTenantPermissionAsync(tenantAttr.Permission, permissionsContext);
        }
        if (attr is RequireTenantPermission tenantAttr2)
        {
            return await CheckTenantPermissionAsync(tenantAttr2.Permission, permissionsContext);
        }

        // Handle simple permission attributes
        if (attr is RequiresPermissionAttribute simpleAttr)
        {
            return await permissionsContext.HasTenantPermissionAsync(simpleAttr.PermissionName);
        }
        if (attr is RequirePermissionAttribute simpleAttr2)
        {
            return await permissionsContext.HasTenantPermissionAsync(simpleAttr2.PermissionName);
        }

        return true;
    }

    private async Task<bool> CheckResourcePermissionAsync(
        IResourcePermissionMarker attr,
        HttpContext httpContext,
        IPermissionsContext permissionsContext)
    {
        // Extract resource ID from route/query
        var resourceId = ExtractResourceId(httpContext, attr.ResourceIdParameterName);
        if (resourceId == null)
        {
            _logger.LogWarning(
                "Resource ID parameter '{Parameter}' not found in request",
                attr.ResourceIdParameterName);
            return false;
        }

        // Build permission name from enum value
        var permissionName = $"{attr.ResourceType.Name}.{attr.RequiredPermission}";
        
        // Check resource-level permission using IPermissionsContext
        var hasPermission = await permissionsContext.HasResourcePermissionAsync(
            attr.ResourceType.Name,
            resourceId.Value,
            permissionName);

        _logger.LogDebug(
            "Resource permission check: User {UserId}, Resource {ResourceType}:{ResourceId}, Permission {Permission} = {Result}",
            permissionsContext.UserId,
            attr.ResourceType.Name,
            resourceId,
            permissionName,
            hasPermission);

        return hasPermission;
    }

    private async Task<bool> CheckContentTypePermissionAsync(
        IContentTypePermissionMarker attr,
        IPermissionsContext permissionsContext)
    {
        // Build permission name from content type and permission
        var permissionName = $"{attr.ResourceType.Name}.{attr.Permission}";
        
        // Check if user has content-type level permission (treated as tenant permission)
        var hasPermission = await permissionsContext.HasTenantPermissionAsync(permissionName);

        _logger.LogDebug(
            "Content-type permission check: User {UserId}, ContentType {ContentType}, Permission {Permission} = {Result}",
            permissionsContext.UserId,
            attr.ResourceType.Name,
            attr.Permission,
            hasPermission);

        return hasPermission;
    }

    private async Task<bool> CheckTenantPermissionAsync(
        object permission,
        IPermissionsContext permissionsContext)
    {
        // Build permission name
        var permissionName = permission.ToString() ?? "";
        
        // Check if user has tenant-level permission
        var hasPermission = await permissionsContext.HasTenantPermissionAsync(permissionName);

        _logger.LogDebug(
            "Tenant permission check: User {UserId}, Permission {Permission} = {Result}",
            permissionsContext.UserId,
            permissionName,
            hasPermission);

        return hasPermission;
    }

    private static Guid? ExtractResourceId(HttpContext httpContext, string parameterName)
    {
        // Try route values first
        if (httpContext.Request.RouteValues.TryGetValue(parameterName, out var routeValue))
        {
            if (Guid.TryParse(routeValue?.ToString(), out var routeGuid))
                return routeGuid;
        }

        // Try query string
        if (httpContext.Request.Query.TryGetValue(parameterName, out var queryValue))
        {
            if (Guid.TryParse(queryValue.FirstOrDefault(), out var queryGuid))
                return queryGuid;
        }

        return null;
    }
}

/// <summary>
///     Extension methods to register the resource permission authorization filter.
/// </summary>
public static class ResourcePermissionAuthorizationExtensions
{
    /// <summary>
    ///     Adds the resource permission authorization filter to MVC.
    /// </summary>
    public static IServiceCollection AddResourcePermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<ResourcePermissionAuthorizationFilter>();
        return services;
    }
}
