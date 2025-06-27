using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using GameGuild.Common.Services;
using GameGuild.Common.Entities;
using GameGuild.Modules.Comment.Models;
using GameGuild.Modules.Product.Models;
using GameGuild.Modules.Project.Models;
using GameGuild.Modules.Auth.Constants;

namespace GameGuild.Common.Attributes;

/// <summary>
/// Generic attribute for resource-level permission checks. This implementation provides
/// hierarchical permission checking: Resource → Content-Type → Tenant
/// </summary>
/// <typeparam name="TPermission">The permission entity type (e.g., CommentPermission, ProductPermission)</typeparam>
/// <typeparam name="TResource">The resource entity type (e.g., Comment, Product)</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute<TPermission, TResource> : Attribute, IAsyncAuthorizationFilter where TPermission : ResourcePermission<TResource> where TResource : BaseEntity {
  private readonly PermissionType _requiredPermission;

  private readonly string _resourceIdParameterName;

  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") {
    _requiredPermission = requiredPermission;
    _resourceIdParameterName = resourceIdParameterName;
  }

  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

    // Extract user ID and tenant ID from JWT token
    string? userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(userIdClaim, out Guid userId)) {
      context.Result = new UnauthorizedResult();

      return;
    }

    // Extract tenant ID - this is optional for global users
    string? tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;
    Guid? tenantId = null;

    if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out Guid parsedTenantId)) { tenantId = parsedTenantId; }

    // Extract resource ID from route parameters
    var resourceIdValue = context.RouteData.Values[_resourceIdParameterName]?.ToString();

    if (!Guid.TryParse(resourceIdValue, out Guid resourceId)) {
      context.Result = new BadRequestResult();

      return;
    } // Hierarchical permission checking: Resource → Content-Type → Tenant

    // Step 1 - Check resource-level permission using generic types
    try {
      bool hasResourcePermission = await permissionService.HasResourcePermissionAsync<TPermission, TResource>(userId, tenantId, resourceId, _requiredPermission);

      if (hasResourcePermission) {
        return; // Permission granted at resource level
      }
    }
    catch {
      // If resource-level checking fails, continue to content-type fallback
    }

    // Step 2 - Check content-type level permission (fallback)
    string contentTypeName = typeof(TResource).Name;
    bool hasContentTypePermission = await permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, _requiredPermission);

    if (hasContentTypePermission) {
      return; // Permission granted at content-type level
    }

    // Step 3 - Check tenant-level permission (final fallback)
    bool hasTenantPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, _requiredPermission);

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
public class RequireResourcePermissionAttribute<TResource> : Attribute, IAsyncAuthorizationFilter where TResource : BaseEntity {
  private readonly PermissionType _requiredPermission;

  private readonly string _resourceIdParameterName;

  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameterName = "id") {
    _requiredPermission = requiredPermission;
    _resourceIdParameterName = resourceIdParameterName;
  }

  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    try {
      var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();

      // Extract user ID and tenant ID from JWT token
      string? userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      if (!Guid.TryParse(userIdClaim, out Guid userId)) {
        context.Result = new UnauthorizedResult();

        return;
      }

      // Extract tenant ID - this is optional for global users
      string? tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;
      Guid? tenantId = null;

      if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out Guid parsedTenantId)) { tenantId = parsedTenantId; } // Extract resource ID from route parameters

      var resourceIdValue = context.RouteData.Values[_resourceIdParameterName]?.ToString();
      Guid resourceId = Guid.Empty;
      bool hasResourceId = Guid.TryParse(resourceIdValue, out resourceId);

      // For CREATE operations or READ operations on collections, we typically don't have a resource ID yet
      // So we skip resource-level permission checks and go to content-type/tenant checks
      if (!hasResourceId && (_requiredPermission == PermissionType.Create || _requiredPermission == PermissionType.Read)) {
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
          string resourceTypeName = typeof(TResource).Name;

          switch (resourceTypeName) {
            case "Comment":
              bool hasCommentPermission = await permissionService.HasResourcePermissionAsync<CommentPermission, Comment>(userId, tenantId, resourceId, _requiredPermission);

              if (hasCommentPermission) {
                return; // Permission granted at resource level
              }

              break;

            case "Product":
              bool hasProductPermission = await permissionService.HasResourcePermissionAsync<ProductPermission, Product>(userId, tenantId, resourceId, _requiredPermission);

              if (hasProductPermission) {
                return; // Permission granted at resource level
              }

              break;

            case "Project":
              bool hasProjectPermission = await permissionService.HasResourcePermissionAsync<ProjectPermission, GameGuild.Modules.Project.Models.Project>(userId, tenantId, resourceId, _requiredPermission);

              if (hasProjectPermission) {
                return; // Permission granted at resource level
              }

              break; // Add more resource types here as they are implemented

            case "Program":
              var hasProgramPermission = await permissionService.HasResourcePermissionAsync<GameGuild.Modules.Program.Models.ProgramPermission, GameGuild.Modules.Program.Models.Program>(userId, tenantId, resourceId, _requiredPermission);

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
      string contentTypeName = typeof(TResource).Name;
      bool hasContentTypePermission = await permissionService.HasContentTypePermissionAsync(userId, tenantId, contentTypeName, _requiredPermission);

      if (hasContentTypePermission) {
        return; // Permission granted at content-type level
      }

      // Step 3 - Check tenant-level permission (final fallback)
      bool hasTenantPermission = await permissionService.HasTenantPermissionAsync(userId, tenantId, _requiredPermission);

      if (!hasTenantPermission) { context.Result = new ForbidResult(); }
      // If we reach here with tenant permission, access is granted
    }
    catch (Exception ex) {
      // Log the exception and return 500 error
      var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireResourcePermissionAttribute<TResource>>>();
      logger?.LogError(ex, "Authorization error in RequireResourcePermissionAttribute");
      context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult("Authorization error occurred") { StatusCode = 500 };
    }
  }
}
