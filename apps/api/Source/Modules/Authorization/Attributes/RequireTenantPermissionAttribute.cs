using System.Security.Claims;
using GameGuild.Modules.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild;

/// <summary> Attribute for tenant-level permission checks. Validates that the user has the specified permission at the tenant level based on their JWT token. Now enhanced with DAC resolver for better permission hierarchy resolution. </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireTenantPermissionAttribute(PermissionType requiredPermission) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Use simplified permission service
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

        // Extract user ID and tenant ID from JWT token
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        var tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Result = new UnauthorizedResult();

            return;
        }

        bool hasPermission;

        try
        {
            // Use simplified permission service
            hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, requiredPermission);
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireTenantPermissionAttribute>>();
            logger?.LogError(ex, "Error checking tenant permission {Permission} for user {UserId}", requiredPermission, userId);

            context.Result = new StatusCodeResult(500);

            return;
        }

        if (!hasPermission) { context.Result = new PermissionDeniedResult(requiredPermission.ToString()); }
    }
}
