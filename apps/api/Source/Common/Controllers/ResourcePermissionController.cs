using GameGuild.Common.Attributes;
using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Common.Controllers;

/// <summary>
/// Controller for managing resource-level permissions and sharing
/// Provides flexible permission management across different resource types
/// </summary>
[ApiController]
[Route("api/resources/{resourceType}/{resourceId}/permissions")]
[Authorize]
public class ResourcePermissionController : ControllerBase
{
    private readonly IDacPermissionResolver _permissionResolver;
    private readonly IResourcePermissionService _resourcePermissionService;
    private readonly ILogger<ResourcePermissionController> _logger;

    public ResourcePermissionController(
        IDacPermissionResolver permissionResolver,
        IResourcePermissionService resourcePermissionService,
        ILogger<ResourcePermissionController> logger)
    {
        _permissionResolver = permissionResolver;
        _resourcePermissionService = resourcePermissionService;
        _logger = logger;
    }

    /// <summary>
    /// Get effective permissions for current user on a resource
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<IEnumerable<EffectivePermission>>> GetMyPermissions(
        string resourceType, 
        Guid resourceId)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        var permissions = await GetEffectivePermissionsByResourceType(
            resourceType, userId, tenantId, resourceId);

        return Ok(permissions);
    }

    /// <summary>
    /// Get all users with access to a resource and their permissions
    /// </summary>
    [HttpGet("users")]
    [RequireDacPermission(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<ResourceUserPermission>>> GetResourceUsers(
        string resourceType, 
        Guid resourceId)
    {
        var userId = GetCurrentUserId();

        // Check if user can view permissions for this resource
        var canView = await CheckResourcePermission(resourceType, resourceId, PermissionType.Read);
        if (!canView)
            return Forbid("You don't have permission to view users for this resource");

        var users = await _resourcePermissionService.GetResourceUsersAsync(
            resourceType, resourceId, userId);

        return Ok(users);
    }

    /// <summary>
    /// Share a resource with users
    /// </summary>
    [HttpPost("share")]
    [RequireDacPermission(PermissionType.Share)]
    public async Task<ActionResult<ShareResult>> ShareResource(
        string resourceType,
        Guid resourceId,
        [FromBody] ShareResourceRequest shareRequest)
    {
        var userId = GetCurrentUserId();

        // Check if user can share this resource
        var canShare = await CheckResourcePermission(resourceType, resourceId, PermissionType.Share);
        if (!canShare)
            return Forbid("You don't have permission to share this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await _permissionResolver.CanGrantPermissionsAsync(
            userId, GetCurrentTenantId(), shareRequest.Permissions, resourceId);

        if (!canGrantPermissions)
            return Forbid("You don't have permission to grant some of the requested permissions");

        var result = await _resourcePermissionService.ShareResourceAsync(
            resourceType, resourceId, shareRequest, userId);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result);
    }

    /// <summary>
    /// Update permissions for a specific user on a resource
    /// </summary>
    [HttpPut("users/{targetUserId}")]
    [RequireDacPermission(PermissionType.Edit)]
    public async Task<ActionResult<PermissionUpdateResult>> UpdateUserPermissions(
        string resourceType,
        Guid resourceId,
        Guid targetUserId,
        [FromBody] UpdatePermissionsRequest request)
    {
        var userId = GetCurrentUserId();

        // Check if user can manage permissions for this resource
        var canManage = await CheckResourcePermission(resourceType, resourceId, PermissionType.Edit);
        if (!canManage)
            return Forbid("You don't have permission to manage permissions for this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await _permissionResolver.CanGrantPermissionsAsync(
            userId, GetCurrentTenantId(), request.Permissions, resourceId);

        if (!canGrantPermissions)
            return Forbid("You don't have permission to grant some of the requested permissions");

        var result = await _resourcePermissionService.UpdateUserPermissionsAsync(
            resourceType, resourceId, targetUserId, request.Permissions, userId, request.ExpiresAt);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result);
    }

    /// <summary>
    /// Remove a user's access to a resource
    /// </summary>
    [HttpDelete("users/{targetUserId}")]
    [RequireDacPermission(PermissionType.Edit)]
    public async Task<ActionResult<PermissionUpdateResult>> RemoveUserAccess(
        string resourceType,
        Guid resourceId,
        Guid targetUserId)
    {
        var userId = GetCurrentUserId();

        // Check if user can manage permissions for this resource
        var canManage = await CheckResourcePermission(resourceType, resourceId, PermissionType.Edit);
        if (!canManage)
            return Forbid("You don't have permission to manage permissions for this resource");

        var result = await _resourcePermissionService.RemoveUserAccessAsync(
            resourceType, resourceId, targetUserId, userId);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result);
    }

    /// <summary>
    /// Invite a user to access a resource
    /// </summary>
    [HttpPost("invite")]
    [RequireDacPermission(PermissionType.Share)]
    public async Task<ActionResult<InvitationResult>> InviteUser(
        string resourceType,
        Guid resourceId,
        [FromBody] InviteUserRequest inviteRequest)
    {
        var userId = GetCurrentUserId();

        // Check if user can share this resource
        var canShare = await CheckResourcePermission(resourceType, resourceId, PermissionType.Share);
        if (!canShare)
            return Forbid("You don't have permission to invite users to this resource");

        // Validate that user can grant the requested permissions
        var canGrantPermissions = await _permissionResolver.CanGrantPermissionsAsync(
            userId, GetCurrentTenantId(), inviteRequest.Permissions, resourceId);

        if (!canGrantPermissions)
            return Forbid("You don't have permission to grant some of the requested permissions");

        var result = await _resourcePermissionService.InviteUserToResourceAsync(
            resourceType, resourceId, inviteRequest, userId);

        if (!result.Success)
            return BadRequest(result.ErrorMessage);

        return Ok(result);
    }

    /// <summary>
    /// Get pending invitations for a resource
    /// </summary>
    [HttpGet("invitations")]
    [RequireDacPermission(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<ResourceInvitation>>> GetPendingInvitations(
        string resourceType,
        Guid resourceId)
    {
        var userId = GetCurrentUserId();

        // Check if user can view invitations for this resource
        var canView = await CheckResourcePermission(resourceType, resourceId, PermissionType.Read);
        if (!canView)
            return Forbid("You don't have permission to view invitations for this resource");

        var invitations = await _resourcePermissionService.GetPendingInvitationsAsync(
            resourceType, resourceId, userId);

        return Ok(invitations);
    }

    /// <summary>
    /// Get detailed permission hierarchy for debugging
    /// </summary>
    [HttpGet("hierarchy")]
    [RequireDacPermission(PermissionType.Read)]
    public async Task<ActionResult<PermissionHierarchy>> GetPermissionHierarchy(
        string resourceType,
        Guid resourceId,
        [FromQuery] PermissionType permission)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        var hierarchy = await GetPermissionHierarchyByResourceType(
            resourceType, userId, tenantId, permission, resourceId);

        return Ok(hierarchy);
    }

    #region Private Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private Guid? GetCurrentTenantId()
    {
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
    }

    private async Task<bool> CheckResourcePermission(string resourceType, Guid resourceId, PermissionType permission)
    {
        var userId = GetCurrentUserId();
        var tenantId = GetCurrentTenantId();

        var result = await GetPermissionResultByResourceType(
            resourceType, userId, tenantId, permission, resourceId);

        return result.IsGranted;
    }

    private async Task<PermissionResult> GetPermissionResultByResourceType(
        string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "projects" => await _permissionResolver.ResolvePermissionAsync<Project>(
                userId, tenantId, permission, resourceId, "Project"),
            "posts" => await _permissionResolver.ResolvePermissionAsync<Entity>(
                userId, tenantId, permission, resourceId, "Post"),
            "contents" => await _permissionResolver.ResolvePermissionAsync<Entity>(
                userId, tenantId, permission, resourceId, "Content"),
            "products" => await _permissionResolver.ResolvePermissionAsync<Entity>(
                userId, tenantId, permission, resourceId, "Product"),
            "resources" => await _permissionResolver.ResolvePermissionAsync<Entity>(
                userId, tenantId, permission, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    private async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsByResourceType(
        string resourceType, Guid userId, Guid? tenantId, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "projects" => await _permissionResolver.GetEffectivePermissionsAsync<Project>(
                userId, tenantId, resourceId, "Project"),
            "posts" => await _permissionResolver.GetEffectivePermissionsAsync<Entity>(
                userId, tenantId, resourceId, "Post"),
            "contents" => await _permissionResolver.GetEffectivePermissionsAsync<Entity>(
                userId, tenantId, resourceId, "Content"),
            "products" => await _permissionResolver.GetEffectivePermissionsAsync<Entity>(
                userId, tenantId, resourceId, "Product"),
            "resources" => await _permissionResolver.GetEffectivePermissionsAsync<Entity>(
                userId, tenantId, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    private async Task<PermissionHierarchy> GetPermissionHierarchyByResourceType(
        string resourceType, Guid userId, Guid? tenantId, PermissionType permission, Guid resourceId)
    {
        return resourceType.ToLower() switch
        {
            "projects" => await _permissionResolver.GetPermissionHierarchyAsync<Project>(
                userId, tenantId, permission, resourceId, "Project"),
            "posts" => await _permissionResolver.GetPermissionHierarchyAsync<Entity>(
                userId, tenantId, permission, resourceId, "Post"),
            "contents" => await _permissionResolver.GetPermissionHierarchyAsync<Entity>(
                userId, tenantId, permission, resourceId, "Content"),
            "products" => await _permissionResolver.GetPermissionHierarchyAsync<Entity>(
                userId, tenantId, permission, resourceId, "Product"),
            "resources" => await _permissionResolver.GetPermissionHierarchyAsync<Entity>(
                userId, tenantId, permission, resourceId, "Resource"),
            _ => throw new ArgumentException($"Unknown resource type: {resourceType}"),
        };
    }

    #endregion
}

/// <summary>
/// Request to update user permissions
/// </summary>
public class UpdatePermissionsRequest
{
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
    public DateTime? ExpiresAt { get; set; }
}
