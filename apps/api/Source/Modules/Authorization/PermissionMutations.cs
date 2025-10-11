using System.Security.Claims;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Permissions;

namespace GameGuild.GraphQL;

/// <summary> GraphQL mutations for permission management </summary>
[ExtendObjectType<Mutation>]
public class PermissionMutations {
    /// <summary> Share a resource with specific users </summary>
    [HotChocolate.Authorization.Authorize]
    public async Task<ShareResult> ShareResource([Service] IResourcePermissionService service, [Service] IPermissionResolver resolver, [Service] IHttpContextAccessor httpContextAccessor, ShareResourceInput input) {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can share this resource
        var canShare = await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, PermissionType.Share, input.ResourceId, input.ResourceType);

        if (!canShare.IsGranted) throw new UnauthorizedAccessException("You don't have permission to share this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await resolver.CanGrantPermissionsAsync(userId, tenantId, input.Permissions, input.ResourceId);

        if (!canGrantPermissions) throw new UnauthorizedAccessException("You don't have permission to grant some of the requested permissions");

        var shareRequest = new ShareResourceRequest {
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

    /// <summary> Update user permissions on a resource </summary>
    [HotChocolate.Authorization.Authorize]
    public async Task<PermissionUpdateResult> UpdateUserPermissions(
        [Service] IResourcePermissionService service,
        [Service] IPermissionResolver resolver,
        [Service] IHttpContextAccessor httpContextAccessor,
        UpdateUserPermissionsInput input
    ) {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can manage permissions for this resource
        var canManage = await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, PermissionType.Edit, input.ResourceId, input.ResourceType);

        if (!canManage.IsGranted) throw new UnauthorizedAccessException("You don't have permission to manage permissions for this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await resolver.CanGrantPermissionsAsync(userId, tenantId, input.Permissions, input.ResourceId);

        if (!canGrantPermissions) throw new UnauthorizedAccessException("You don't have permission to grant some of the requested permissions");

        return await service.UpdateUserPermissionsAsync(input.ResourceType, input.ResourceId, input.TargetUserId, input.Permissions, userId, input.ExpiresAt);
    }

    /// <summary> Remove user access from a resource </summary>
    [HotChocolate.Authorization.Authorize]
    public async Task<PermissionUpdateResult> RemoveUserAccess([Service] IResourcePermissionService service, [Service] IPermissionResolver resolver, [Service] IHttpContextAccessor httpContextAccessor, RemoveUserAccessInput input) {
        var context = httpContextAccessor.HttpContext!;
        var userId = GetUserIdFromContext(context);
        var tenantId = GetTenantIdFromContext(context);

        // Check if user can manage permissions for this resource
        var canManage = await resolver.ResolvePermissionAsync<EntityBase>(userId, tenantId, PermissionType.Edit, input.ResourceId, input.ResourceType);

        if (!canManage.IsGranted) throw new UnauthorizedAccessException("You don't have permission to manage permissions for this resource");

        return await service.RemoveUserAccessAsync(input.ResourceType, input.ResourceId, input.TargetUserId, userId);
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

    #endregion
}
