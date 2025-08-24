using System.Security.Claims;
using GameGuild.Common.Results;
using GameGuild.Common.Services;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameGuild.Common.Attributes;

/// <summary>
/// Enhanced attribute for DAC permission checks across all 3 layers
/// Supports tenant, content-type, and resource-level permissions
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireDacPermissionAttribute : Attribute, IAsyncAuthorizationFilter {
  public PermissionType RequiredPermission { get; }
  public string? ResourceIdParameter { get; set; }
  public string? ContentTypeName { get; set; }
  public bool AllowOwnerOverride { get; set; } = true;
  public string? ResourceOwnerIdProperty { get; set; }

  public RequireDacPermissionAttribute(PermissionType requiredPermission) {
    RequiredPermission = requiredPermission;
  }

  public async Task OnAuthorizationAsync(AuthorizationFilterContext context) {
    var resolver = context.HttpContext.RequestServices.GetRequiredService<IDacPermissionResolver>();

    // Extract user ID and tenant ID from JWT token
    var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId)) {
      context.Result = new UnauthorizedResult();
      return;
    }

    var tenantIdClaim = context.HttpContext.User.FindFirst(JwtClaimTypes.TenantId)?.Value;
    Guid.TryParse(tenantIdClaim, out var tenantId);

    // Extract resource ID if specified
    Guid? resourceId = null;
    if (!string.IsNullOrEmpty(ResourceIdParameter)) {
      var resourceIdValue = context.RouteData.Values[ResourceIdParameter]?.ToString() ??
                           context.HttpContext.Request.Query[ResourceIdParameter].FirstOrDefault();

      if (Guid.TryParse(resourceIdValue, out var parsedResourceId)) {
        resourceId = parsedResourceId;
      }
    }

    try {
      // Check DAC permission
      var permissionResult = await resolver.ResolvePermissionAsync<Entity>(
          userId, tenantId, RequiredPermission, resourceId, ContentTypeName);

      if (permissionResult.IsGranted) {
        // Permission granted through DAC
        return;
      }

      // Check owner override if enabled
      if (AllowOwnerOverride && resourceId.HasValue && !string.IsNullOrEmpty(ResourceOwnerIdProperty)) {
        var isOwner = await CheckResourceOwnership(context, userId, resourceId.Value);
        if (isOwner) {
          return;
        }
      }

      // Permission denied
      context.Result = new PermissionDeniedResult(RequiredPermission.ToString());
    }
    catch (Exception ex) {
      var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireDacPermissionAttribute>>();
      logger?.LogError(ex, "Error checking DAC permission {Permission} for user {UserId}",
          RequiredPermission, userId);

      context.Result = new StatusCodeResult(500);
    }
  }

  private async Task<bool> CheckResourceOwnership(AuthorizationFilterContext context, Guid userId, Guid resourceId) {
    // This would need to be implemented based on your resource ownership logic
    // For now, returning false - implement based on your specific resource models
    return false;
  }
}

/// <summary>
/// Attribute specifically for resource-level permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireResourcePermissionAttribute : RequireDacPermissionAttribute {
  public RequireResourcePermissionAttribute(PermissionType requiredPermission, string resourceIdParameter)
      : base(requiredPermission) {
    ResourceIdParameter = resourceIdParameter;
  }
}

/// <summary>
/// Attribute specifically for content-type permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireContentTypePermissionAttribute : RequireDacPermissionAttribute {
  public RequireContentTypePermissionAttribute(PermissionType requiredPermission, string contentTypeName)
      : base(requiredPermission) {
    ContentTypeName = contentTypeName;
  }
}

/// <summary>
/// Attribute for project-specific permission checks
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireProjectPermissionAttribute : RequireResourcePermissionAttribute {
  public RequireProjectPermissionAttribute(PermissionType requiredPermission)
      : base(requiredPermission, "projectId") {
    ContentTypeName = "Project";
    ResourceOwnerIdProperty = "OwnerId";
  }
}
