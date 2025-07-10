using System.Security.Claims;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Comments;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Products.Models;
using GameGuild.Modules.Programs.Models;
using GameGuild.Modules.Projects.Models;
using GameGuild.Modules.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace GameGuild.Common;

/// <summary>
/// Generic attribute for resource-level permission checks. This implementation provides
/// hierarchical permission checking: Resource → Content-Type → Tenant
/// </summary>
/// <typeparam name="TPermission">The permission entity type (e.g., CommentPermission, ProductPermission)</typeparam>
/// <typeparam name="TResource">The resource entity type (e.g., Comment, Product)</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TPermission, TResource>(PermissionType requiredPermission, string resourceIdParameterName = "id") : Attribute, IAsyncAuthorizationFilter
  where TPermission : ResourcePermission<TResource>
  where TResource : Entity {
  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

    // Extract user ID and tenant ID from JWT token
    var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userIdClaim, out var userId)) {
      context.Result = new UnauthorizedResult();

      return;
    }

    // Extract tenant ID - this is optional for global users
    var tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;
    Guid? tenantId = null;

    if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId)) { tenantId = parsedTenantId; }

    // Extract resource ID from route parameters
    var resourceIdValue = context.RouteData.Values[resourceIdParameterName]?.ToString();

    if (!Guid.TryParse(resourceIdValue, out var resourceId)) {
      context.Result = new BadRequestResult();

      return;
    } // Hierarchical permission checking: Resource → Content-Type → Tenant

    // Step 1 - Check resource-level permission using generic types
    try {
      var hasResourcePermission =
        await permissionService.HasResourcePermissionAsync<TPermission, TResource>(
          userId,
          tenantId,
          resourceId,
          requiredPermission
        );

      if (hasResourcePermission) {
        return; // Permission granted at resource level
      }
    }
    catch {
      // If resource-level checking fails, continue to content-type fallback
    }

    // Step 2 - Check content-type level permission (fallback)
    var contentTypeName = typeof(TResource).Name;
    var hasContentTypePermission =
      await permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, requiredPermission);

    if (hasContentTypePermission) {
      return; // Permission granted at content-type level
    }

    // Step 3 - Check tenant-level permission (final fallback)
    var hasTenantPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, requiredPermission);

    if (!hasTenantPermission) { context.Result = new ForbidResult(); }

    // If we reach here with tenant permission, access is granted
  }
}

/// <summary>
/// Backward-compatible generic attribute for resource-level permission checks.
/// This version automatically infers the permission type based on naming convention.
/// </summary>
/// <typeparam name="TResource">The resource entity type (e.g., Comment, Product)</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TResource>(PermissionType requiredPermission, string resourceIdParameterName = "id") : Attribute, IAsyncAuthorizationFilter
  where TResource : Entity {
  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    try {
      var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

      // Extract user ID and tenant ID from JWT token
      var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (!Guid.TryParse(userIdClaim, out var userId)) {
        context.Result = new UnauthorizedResult();

        return;
      }

      // Extract tenant ID - this is optional for global users
      var tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;
      Guid? tenantId = null;

      if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var parsedTenantId)) { tenantId = parsedTenantId; } // Extract resource ID from route parameters

      var resourceIdValue = context.RouteData.Values[resourceIdParameterName]?.ToString();
      var resourceId = Guid.Empty;
      var hasResourceId = Guid.TryParse(resourceIdValue, out resourceId);

      // For CREATE operations or READ operations on collections, we typically don't have a resource ID yet
      // So we skip resource-level permission checks and go to content-type/tenant checks
      if (!hasResourceId &&
          (requiredPermission == PermissionType.Create || requiredPermission == PermissionType.Read)) {
        // Skip resource-level check for CREATE operations or READ collection operations
      }
      else if (!hasResourceId) {
        // For other operations (UPDATE, DELETE), we need a valid resource ID
        context.Result = new BadRequestResult();

        return;
      }

      // Hierarchical permission checking: Resource → Content-Type → Tenant

      // Step 1 - Check resource-level permission for known resource types
      // Skip this step for CREATE operations without a resource ID
      if (hasResourceId) {
        try {
          var resourceTypeName = typeof(TResource).Name;

          switch (resourceTypeName) {
            case "Comment":
              var hasCommentPermission =
                await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(
                  userId,
                  tenantId,
                  resourceId,
                  requiredPermission
                );

              if (hasCommentPermission) {
                return; // Permission granted at resource level
              }

              break;

            case "Product":
              var hasProductPermission =
                await permissionService.HasResourcePermissionAsync<ProductPermission, Product>(
                  userId,
                  tenantId,
                  resourceId,
                  requiredPermission
                );

              if (hasProductPermission) {
                return; // Permission granted at resource level
              }

              break;

            case "Project":
              var hasProjectPermission =
                await permissionService
                  .HasResourcePermissionAsync<ProjectPermission, Project>(
                    userId,
                    tenantId,
                    resourceId,
                    requiredPermission
                  );

              if (hasProjectPermission) {
                return; // Permission granted at resource level
              }

              break; // Add more resource types here as they are implemented

            case "Program":
              var hasProgramPermission =
                await permissionService
                  .HasResourcePermissionAsync<ProgramPermission,
                    Modules.Programs.Models.Program>(userId, tenantId, resourceId, requiredPermission);

              if (hasProgramPermission) {
                return; // Permission granted at resource level
              }

              break;

            default:
              // For unknown resource types, skip resource-level check and go to content-type fallback
              break;
          }
        }
        catch {
          // If resource-level checking fails, continue to content-type fallback
        }
      } // End of hasResourceId check

      // Step 2 - Check content-type level permission (fallback)
      var contentTypeName = typeof(TResource).Name;
      var hasContentTypePermission =
        await permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, requiredPermission);

      if (hasContentTypePermission) {
        return; // Permission granted at content-type level
      }

      // Step 3 - Check tenant-level permission (final fallback)
      var hasTenantPermission =
        await permissionService.HasTenantPermissionAsync(userId, tenantId, requiredPermission);

      if (!hasTenantPermission) { context.Result = new ForbidResult(); }
      // If we reach here with tenant permission, access is granted
    }
    catch (Exception ex) {
      // Log the exception and return 500 error
      var logger = context.HttpContext.RequestServices
                          .GetService<ILogger<RequireResourcePermissionAttribute<TResource>>>();
      logger?.LogError(ex, "Authorization error in RequireResourcePermissionAttribute");
      context.Result = new ObjectResult("Authorization error occurred") { StatusCode = 500 };
    }
  }
}
