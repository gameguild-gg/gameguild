using System.Security.Claims;
using GameGuild.Common.Application.Services;
using GameGuild.Modules.Authentication.Constants;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace GameGuild.Common.Application.Attributes;

/// <summary>
/// Generic attribute for content-type (table) level permission checks. Validates that the user 
/// has the specified permission for the given content type, with hierarchical fallback to tenant-level.
/// </summary>
/// <typeparam name="T">The entity type representing the content-type/table</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireContentTypePermissionAttribute<T>(PermissionType requiredPermission) : Attribute, IAsyncAuthorizationFilter
  where T : class {
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

    // Get content type name from generic type parameter
    var contentTypeName = typeof(T).Name;

    // Check content-type level permission with hierarchical fallback
    var hasPermission =
      await permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, requiredPermission);

    if (!hasPermission) context.Result = new ForbidResult();
  }
}
