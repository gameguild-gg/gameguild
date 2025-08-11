using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Projects;
using HotChocolate.Authorization;
using System.Security.Claims;
using AuthorizeAttribute = HotChocolate.Authorization.AuthorizeAttribute;

namespace GameGuild.Common.GraphQL;

/// <summary>
/// GraphQL extensions for permission management and querying
/// </summary>
[ExtendObjectType<Query>]
public class PermissionQueries
{
    /// <summary>
    /// Get effective permissions for current user on a resource
    /// </summary>
    [Authorize]
    public async Task<IEnumerable<EffectivePermission>> GetEffectivePermissions(
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        string resourceType,
        Guid resourceId,
        string? userId = null)
    {
        var context = httpContextAccessor.HttpContext!;
        var currentUserId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);
        
        var targetUserId = string.IsNullOrEmpty(userId) ? currentUserId : Guid.Parse(userId);

        // Check if user can view permissions for this resource
        var canView = await resolver.ResolvePermissionAsync<Entity>(
            currentUserId, tenantId, PermissionType.Read, resourceId, resourceType);
            
        if (!canView.IsGranted)
            throw new UnauthorizedAccessException("You don't have permission to view permissions for this resource");

        return await GetEffectivePermissionsByType(resolver, resourceType, targetUserId, tenantId, resourceId);
    }

    /// <summary>
    /// Get permission hierarchy for debugging and understanding permission resolution
    /// </summary>
    [Authorize]
    public async Task<PermissionHierarchy> GetPermissionHierarchy(
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        string resourceType,
        Guid resourceId,
        PermissionType permission,
        string? userId = null)
    {
        var context = httpContextAccessor.HttpContext!;
        var currentUserId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);
        
        var targetUserId = string.IsNullOrEmpty(userId) ? currentUserId : Guid.Parse(userId);

        // Check if user can view permissions for this resource
        var canView = await resolver.ResolvePermissionAsync<Entity>(
            currentUserId, tenantId, PermissionType.Read, resourceId, resourceType);
            
        if (!canView.IsGranted)
            throw new UnauthorizedAccessException("You don't have permission to view permissions for this resource");

        return await GetPermissionHierarchyByType(resolver, resourceType, targetUserId, tenantId, permission, resourceId);
    }

    /// <summary>
    /// Check if current user has specific permission on a resource
    /// </summary>
    [Authorize]
    public async Task<bool> HasPermission(
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        string resourceType,
        Guid resourceId,
        PermissionType permission)
    {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        var result = await GetPermissionResultByType(resolver, resourceType, userId, tenantId, permission, resourceId);
        return result.IsGranted;
    }

    /// <summary>
    /// Get all resources where user has specific permission
    /// </summary>
    [Authorize]
    public async Task<IEnumerable<Guid>> GetResourcesWithPermission(
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        string resourceType,
        PermissionType permission,
        Guid[] resourceIds)
    {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        var results = await BulkResolvePermissionsByType(resolver, resourceType, userId, tenantId, resourceIds, new[] { permission });
        
        return results
            .Where(kvp => kvp.Value.ContainsKey(permission) && kvp.Value[permission].IsGranted)
            .Select(kvp => kvp.Key);
    }

    #region Private Helper Methods

    private static Guid GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("User ID not found");
    }

    private static Guid? GetTenantIdFromContext(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    private static async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsByType(
        IDacPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "project" or "projects" => await resolver.GetEffectivePermissionsAsync<Project>(userId, tenantId, resourceId, "Project"),
            "post" or "posts" => await resolver.GetEffectivePermissionsAsync<Entity>(userId, tenantId, resourceId, "Post"),
            "content" or "contents" => await resolver.GetEffectivePermissionsAsync<Entity>(userId, tenantId, resourceId, "Content"),
            "product" or "products" => await resolver.GetEffectivePermissionsAsync<Entity>(userId, tenantId, resourceId, "Product"),
            "resource" or "resources" => await resolver.GetEffectivePermissionsAsync<Entity>(userId, tenantId, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    private static async Task<PermissionHierarchy> GetPermissionHierarchyByType(
        IDacPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "project" or "projects" => await resolver.GetPermissionHierarchyAsync<Project>(userId, tenantId, permission, resourceId, "Project"),
            "post" or "posts" => await resolver.GetPermissionHierarchyAsync<Entity>(userId, tenantId, permission, resourceId, "Post"),
            "content" or "contents" => await resolver.GetPermissionHierarchyAsync<Entity>(userId, tenantId, permission, resourceId, "Content"),
            "product" or "products" => await resolver.GetPermissionHierarchyAsync<Entity>(userId, tenantId, permission, resourceId, "Product"),
            "resource" or "resources" => await resolver.GetPermissionHierarchyAsync<Entity>(userId, tenantId, permission, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    private static async Task<PermissionResult> GetPermissionResultByType(
        IDacPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "project" or "projects" => await resolver.ResolvePermissionAsync<Project>(userId, tenantId, permission, resourceId, "Project"),
            "post" or "posts" => await resolver.ResolvePermissionAsync<Entity>(userId, tenantId, permission, resourceId, "Post"),
            "content" or "contents" => await resolver.ResolvePermissionAsync<Entity>(userId, tenantId, permission, resourceId, "Content"),
            "product" or "products" => await resolver.ResolvePermissionAsync<Entity>(userId, tenantId, permission, resourceId, "Product"),
            "resource" or "resources" => await resolver.ResolvePermissionAsync<Entity>(userId, tenantId, permission, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    private static async Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>> BulkResolvePermissionsByType(
        IDacPermissionResolver resolver, string resourceType, Guid userId, Guid? tenantId, Guid[] resourceIds, PermissionType[] permissions)
    {
        return resourceType.ToLower() switch
        {
            "project" or "projects" => await resolver.BulkResolvePermissionsAsync<Project>(userId, tenantId, resourceIds, permissions),
            "post" or "posts" => await resolver.BulkResolvePermissionsAsync<Entity>(userId, tenantId, resourceIds, permissions),
            "content" or "contents" => await resolver.BulkResolvePermissionsAsync<Entity>(userId, tenantId, resourceIds, permissions),
            "product" or "products" => await resolver.BulkResolvePermissionsAsync<Entity>(userId, tenantId, resourceIds, permissions),
            "resource" or "resources" => await resolver.BulkResolvePermissionsAsync<Entity>(userId, tenantId, resourceIds, permissions),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    #endregion
}

/// <summary>
/// GraphQL mutations for permission management
/// </summary>
[ExtendObjectType<Mutation>]
public class PermissionMutations
{
    /// <summary>
    /// Share a resource with specific users
    /// </summary>
    [Authorize]
    public async Task<ShareResult> ShareResource(
        [Service] IResourcePermissionService service,
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        ShareResourceInput input)
    {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can share this resource
        var canShare = await resolver.ResolvePermissionAsync<Entity>(
            userId, tenantId, PermissionType.Share, input.ResourceId, input.ResourceType);
            
        if (!canShare.IsGranted)
            throw new UnauthorizedAccessException("You don't have permission to share this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await resolver.CanGrantPermissionsAsync(
            userId, tenantId, input.Permissions, input.ResourceId);

        if (!canGrantPermissions)
            throw new UnauthorizedAccessException("You don't have permission to grant some of the requested permissions");

        var shareRequest = new ShareResourceRequest
        {
            UserEmails = input.UserEmails,
            UserIds = input.UserIds,
            Permissions = input.Permissions,
            ExpiresAt = input.ExpiresAt,
            Message = input.Message,
            RequireAcceptance = input.RequireAcceptance,
            NotifyUsers = input.NotifyUsers,
        };

        return await service.ShareResourceAsync(input.ResourceType, input.ResourceId, shareRequest, userId);
    }

    /// <summary>
    /// Update user permissions on a resource
    /// </summary>
    [Authorize]
    public async Task<PermissionUpdateResult> UpdateUserPermissions(
        [Service] IResourcePermissionService service,
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        UpdateUserPermissionsInput input)
    {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can manage permissions for this resource
        var canManage = await resolver.ResolvePermissionAsync<Entity>(
            userId, tenantId, PermissionType.Edit, input.ResourceId, input.ResourceType);
            
        if (!canManage.IsGranted)
            throw new UnauthorizedAccessException("You don't have permission to manage permissions for this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await resolver.CanGrantPermissionsAsync(
            userId, tenantId, input.Permissions, input.ResourceId);

        if (!canGrantPermissions)
            throw new UnauthorizedAccessException("You don't have permission to grant some of the requested permissions");

        return await service.UpdateUserPermissionsAsync(
            input.ResourceType, input.ResourceId, input.TargetUserId, input.Permissions, userId, input.ExpiresAt);
    }

    /// <summary>
    /// Remove user access from a resource
    /// </summary>
    [Authorize]
    public async Task<PermissionUpdateResult> RemoveUserAccess(
        [Service] IResourcePermissionService service,
        [Service] IDacPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        RemoveUserAccessInput input)
    {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can manage permissions for this resource
        var canManage = await resolver.ResolvePermissionAsync<Entity>(
            userId, tenantId, PermissionType.Edit, input.ResourceId, input.ResourceType);
            
        if (!canManage.IsGranted)
            throw new UnauthorizedAccessException("You don't have permission to manage permissions for this resource");

        return await service.RemoveUserAccessAsync(input.ResourceType, input.ResourceId, input.TargetUserId, userId);
    }

    #region Private Helper Methods

    private static Guid GetUserIdFromContext(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : throw new UnauthorizedAccessException("User ID not found");
    }

    private static Guid? GetTenantIdFromContext(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    #endregion
}

/// <summary>
/// GraphQL input for sharing a resource
/// </summary>
public record ShareResourceInput(
    string ResourceType,
    Guid ResourceId,
    string[] UserEmails,
    Guid[] UserIds,
    PermissionType[] Permissions,
    DateTime? ExpiresAt,
    string? Message,
    bool RequireAcceptance = true,
    bool NotifyUsers = true);

/// <summary>
/// GraphQL input for updating user permissions
/// </summary>
public record UpdateUserPermissionsInput(
    string ResourceType,
    Guid ResourceId,
    Guid TargetUserId,
    PermissionType[] Permissions,
    DateTime? ExpiresAt);

/// <summary>
/// GraphQL input for removing user access
/// </summary>
public record RemoveUserAccessInput(
    string ResourceType,
    Guid ResourceId,
    Guid TargetUserId);
