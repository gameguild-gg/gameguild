using System.Security.Claims;
using GameGuild.Common.Services;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace GameGuild.Common;

/// <summary>
/// Attribute for tenant-level permission checks. Validates that the user has the specified 
/// permission at the tenant level based on their JWT token.
/// Now enhanced with DAC resolver for better permission hierarchy resolution.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireTenantPermissionAttribute(PermissionType requiredPermission) : Attribute, IAsyncAuthorizationFilter {
  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    // Try to use the enhanced DAC resolver first, fallback to legacy service
    var dacResolver = context.HttpContext.RequestServices.GetService<IDacPermissionResolver>();
    var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

    // Extract user ID and tenant ID from JWT token
    var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userIdClaim, out var userId)) {
      context.Result = new UnauthorizedResult();
      return;
    }

    var tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;

    if (!Guid.TryParse(tenantIdClaim, out var tenantId)) {
      context.Result = new UnauthorizedResult();
      return;
    }

    bool hasPermission;

    try {
      if (dacResolver != null) {
        // Use enhanced DAC resolver for better hierarchy resolution
        var result = await dacResolver.ResolvePermissionAsync<Entity>(
          userId, tenantId, requiredPermission);
        hasPermission = result.IsGranted;
      } else {
        // Fallback to legacy permission service
        hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, requiredPermission);
      }
    } catch (Exception ex) {
      var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireTenantPermissionAttribute>>();
      logger?.LogError(ex, "Error checking tenant permission {Permission} for user {UserId}", 
        requiredPermission, userId);
      
      context.Result = new StatusCodeResult(500);
      return;
    }

    if (!hasPermission) {
      context.Result = new ForbidResult();
    }
  }
}
