using System.Security.Claims;
using GameGuild.Common.Application.Services;
using GameGuild.Modules.Authentication.Constants;
using GameGuild.Modules.Comments.Models;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace GameGuild.Common.Application.Attributes;

/// <summary>
/// Specific attribute for Comment resource-level permission checks. This demonstrates 
/// how to implement resource-specific permission checking for concrete entity types.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireCommentPermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") : Attribute, IAsyncAuthorizationFilter {
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

    // Extract resource ID from route parameters
    var resourceIdValue = context.RouteData.Values[resourceIdParameterName]?.ToString();

    if (!Guid.TryParse(resourceIdValue, out var resourceId)) {
      context.Result = new BadRequestResult();

      return;
    }

    // Check resource-level permission with hierarchical fallback
    // This uses the specific CommentPermission and Comment types
    var hasPermission =
      await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
        userId,
        tenantId,
        resourceId,
        requiredPermission
      );

    if (!hasPermission) context.Result = new ForbidResult();
  }
}
