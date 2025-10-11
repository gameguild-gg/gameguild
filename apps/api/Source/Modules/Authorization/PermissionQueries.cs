using System.Security.Claims;
using GameGuild.Core.Domain;
using GameGuild.Modules.Permissions;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;


namespace GameGuild.GraphQL;

/// <summary> GraphQL extensions for permission management and querying </summary>
[ExtendObjectType<Query>]
public class PermissionQueries {
  /// <summary> Get effective permissions for current user on a resource </summary>
  [Authorize]
  public async Task<IEnumerable<EffectivePermission>> GetEffectivePermissions([Service] IPermissionResolver resolver, [Service] IHttpContextAccessor httpContextAccessor, string resourceType, Guid resourceId, string? userId = null) {
    var context = httpContextAccessor.HttpContext!;
    var currentUserId = GetUserIdFromContext(context);
    var tenantId = GetTenantIdFromContext(context);

    var targetUserId = string.IsNullOrEmpty(userId) ? currentUserId : Guid.Parse(userId);

    // Check if user can view permissions for this resource
    var canView = await resolver.ResolvePermissionAsync<EntityBase>(currentUserId, tenantId, PermissionType.Read, resourceId, resourceType);

    if (!canView.IsGranted) throw new UnauthorizedAccessException("You don't have permission to view permissions for this resource");

    return await GetEffectivePermissionsByType(resolver, resourceType, targetUserId, tenantId, resourceId);
  }

  /// <summary> Get permission hierarchy for debugging and understanding permission resolution </summary>
  [Authorize]
  public async Task<PermissionHierarchy> GetPermissionHierarchy(
    [Service] IPermissionResolver resolver,
    [Service] IHttpContextAccessor httpContextAccessor,
    string resourceType,
    Guid resourceId,
    PermissionType permission,
    string? userId = null
  ) {
    var context = httpContextAccessor.HttpContext!;
    var currentUserId = GetUserIdFromContext(context);
    var tenantId = GetTenantIdFromContext(context);

    var targetUserId = string.IsNullOrEmpty(userId) ? currentUserId : Guid.Parse(userId);

    // Check if user can view permissions for this resource
    var canView = await resolver.ResolvePermissionAsync<EntityBase>(currentUserId, tenantId, PermissionType.Read, resourceId, resourceType);

    if (!canView.IsGranted) throw new UnauthorizedAccessException("You don't have permission to view permissions for this resource");

    return await GetPermissionHierarchyByType(resolver, resourceType, targetUserId, tenantId, permission, resourceId);
  }

  /// <summary> Check if current user has specific permission on a resource </summary>
  [Authorize]
  public async Task<bool> HasPermission([Service] IPermissionResolver resolver, [Service] IHttpContextAccessor httpContextAccessor, string resourceType, Guid resourceId, PermissionType permission) {
    var context = httpContextAccessor.HttpContext!;
    var userId = GetUserIdFromContext(context);
    var tenantId = GetTenantIdFromContext(context);

    var result = await GetPermissionResultByType(resolver, resourceType, userId, tenantId, permission, resourceId);

    return result.IsGranted;
  }

  /// <summary> Get all resources where user has specific permission </summary>
  [Authorize]
  public async Task<IEnumerable<Guid>> GetResourcesWithPermission([Service] IPermissionResolver resolver, [Service] IHttpContextAccessor httpContextAccessor, string resourceType, PermissionType permission, Guid[] resourceIds) {
    var context = httpContextAccessor.HttpContext!;
    var userId = GetUserIdFromContext(context);
    var tenantId = GetTenantIdFromContext(context);

    var results = await BulkResolvePermissionsByType(resolver, resourceType, userId, tenantId, resourceIds, [permission]);

    return results.Where(kvp => kvp.Value.ContainsKey(permission) && kvp.Value[permission].IsGranted).Select(kvp => kvp.Key);
  }

  #region Private Helper Methods

  private static Guid GetUserIdFromContext(HttpContext context) {
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    return Guid.TryParse(userIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("User ID not found");
  }

  private static Guid? GetTenantIdFromContext(HttpContext context) {
    var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

    return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
  }

  private static async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsByType(IPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, Guid resourceId) {
    return resourceType.ToLower() switch {
      "project" or "projects" => await resolver.GetEffectivePermissionsAsync<Project>(userId, tenantId, resourceId, "Project"),
      "post" or "posts" => await resolver.GetEffectivePermissionsAsync<EntityBase>(userId, tenantId, resourceId, "Post"),
      "content" or "contents" => await resolver.GetEffectivePermissionsAsync<EntityBase>(userId, tenantId, resourceId, "Content"),
      "product" or "products" => await resolver.GetEffectivePermissionsAsync<EntityBase>(userId, tenantId, resourceId, "Product"),
      "resource" or "resources" => await resolver.GetEffectivePermissionsAsync<EntityBase>(userId, tenantId, resourceId, "Resource"),
      _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
    };
  }

  private static async Task<PermissionHierarchy> GetPermissionHierarchyByType(IPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId) {
    return resourceType.ToLower() switch {
      "project" or "projects" => await resolver.GetPermissionHierarchyAsync<Project>(userId, tenantId, permission, resourceId, "Project"),
      "post" or "posts" => await resolver.GetPermissionHierarchyAsync<EntityBase>(userId, tenantId, permission, resourceId, "Post"),
      "content" or "contents" => await resolver.GetPermissionHierarchyAsync<EntityBase>(userId, tenantId, permission, resourceId, "Content"),
      "product" or "products" => await resolver.GetPermissionHierarchyAsync<EntityBase>(userId, tenantId, permission, resourceId, "Product"),
      "resource" or "resources" => await resolver.GetPermissionHierarchyAsync<EntityBase>(userId, tenantId, permission, resourceId, "Resource"),
      _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
    };
  }

  private static async Task<PermissionResult> GetPermissionResultByType(IPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId) {
    return resourceType.ToLower() switch {
      "project" or "projects" => await resolver.ResolvePermissionAsync<Project>(userId, tenantId, permission, resourceId, "Project"),
      "post" or "posts" => await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, permission, resourceId, "Post"),
      "content" or "contents" => await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, permission, resourceId, "Content"),
      "product" or "products" => await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, permission, resourceId, "Product"),
      "resource" or "resources" => await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, permission, resourceId, "Resource"),
      _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
    };
  }

  private static async Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>>
    BulkResolvePermissionsByType(IPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, Guid[] resourceIds, PermissionType[] permissions) {
    return resourceType.ToLower() switch {
      "project" or "projects" => await resolver.BulkResolvePermissionsAsync<Project>(userId, tenantId, resourceIds, permissions),
      "post" or "posts" => await resolver.BulkResolvePermissionsAsync<EntityBase>(userId, tenantId, resourceIds, permissions),
      "content" or "contents" => await resolver.BulkResolvePermissionsAsync<EntityBase>(userId, tenantId, resourceIds, permissions),
      "product" or "products" => await resolver.BulkResolvePermissionsAsync<EntityBase>(userId, tenantId, resourceIds, permissions),
      "resource" or "resources" => await resolver.BulkResolvePermissionsAsync<EntityBase>(userId, tenantId, resourceIds, permissions),
      _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
    };
  }

  #endregion
}
