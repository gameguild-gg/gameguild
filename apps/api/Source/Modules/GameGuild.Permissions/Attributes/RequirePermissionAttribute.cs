using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace GameGuild.Permissions.Infrastructure.Attributes;

/// <summary>
///     Base authorization attribute that checks permissions using IPermissionsContext
///     Supports tenant, content type, and resource-level permissions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute(string permission, PermissionLayer layer = PermissionLayer.Tenant) : Attribute, IAsyncAuthorizationFilter
{
    public string Permission { get; set; } = permission;

    public PermissionLayer Layer { get; set; } = layer;

    public string? ResourceType { get; set; }

    public string? ResourceIdParameter { get; set; }

    public bool AllowAnonymous { get; set; }

    public bool RequireOwnership { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check for AllowAnonymous
        if (AllowAnonymous) { return; }

        var permissionsContext = context.HttpContext.RequestServices.GetService(typeof(IPermissionsContext)) as IPermissionsContext;

        if (permissionsContext == null)
        {
            context.Result = new StatusCodeResult(500);

            return;
        }

        // Check if user is authenticated
        if (!permissionsContext.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        // System admins bypass permission checks
        if (permissionsContext.IsSystemAdmin) { return; }

        var hasPermission = false;

        switch (Layer)
        {
            case PermissionLayer.Tenant : hasPermission = await permissionsContext.HasTenantPermissionAsync(Permission).ConfigureAwait(false); break;

            case PermissionLayer.Resource : hasPermission = await CheckResourcePermissionAsync(context, permissionsContext).ConfigureAwait(false); break;

            case PermissionLayer.ContentType :
                // Content type is similar to tenant for now
                hasPermission = await permissionsContext.HasTenantPermissionAsync(Permission).ConfigureAwait(false); break;

            case PermissionLayer.Auto :
                // Try resource first, fallback to tenant
                hasPermission = await CheckResourcePermissionAsync(context, permissionsContext).ConfigureAwait(false);

                if (!hasPermission) { hasPermission = await permissionsContext.HasTenantPermissionAsync(Permission).ConfigureAwait(false); }

                break;
        }

        if (!hasPermission) { context.Result = new ForbidResult(); }
    }

    private async Task<bool> CheckResourcePermissionAsync(AuthorizationFilterContext context, IPermissionsContext permissionsContext)
    {
        if (string.IsNullOrEmpty(ResourceType) || string.IsNullOrEmpty(ResourceIdParameter)) { return false; }

        // Try to get resource ID from route, query, or body
        var resourceId = Guid.Empty;

        if (context.RouteData.Values.TryGetValue(ResourceIdParameter, out var routeValue)) { Guid.TryParse(routeValue?.ToString(), out resourceId); }
        else if (context.HttpContext.Request.Query.TryGetValue(ResourceIdParameter, out var queryValue)) { Guid.TryParse(queryValue, out resourceId); }

        if (resourceId == Guid.Empty) { return false; }

        // Check ownership if required
        if (RequireOwnership)
        {
            // TODO: Implement ownership check - need to query resource owner
            // For now, just check permission
        }

        return await permissionsContext.HasResourcePermissionAsync(ResourceType, resourceId, Permission).ConfigureAwait(false);
    }
}
