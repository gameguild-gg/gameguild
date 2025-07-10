using System.Security.Claims;
using GameGuild.Modules.Auth;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace GameGuild.Common;

/// <summary>
/// Attribute for tenant-level permission checks. Validates that the user has the specified 
/// permission at the tenant level based on their JWT token.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireTenantPermissionAttribute(PermissionType requiredPermission) : Attribute, IAsyncAuthorizationFilter {
  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
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

    // Check tenant-level permission
    var hasPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, requiredPermission);

    if (!hasPermission) context.Result = new ForbidResult();
  }
}
