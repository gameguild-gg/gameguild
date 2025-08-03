using GameGuild.Common.Services;
using GameGuild.Modules.Permissions;

namespace GameGuild.Common.Services;

/// <summary>
/// Service for managing resource-level permission sharing and collaboration
/// </summary>
public interface IResourcePermissionService
{
    /// <summary>
    /// Share a resource with specific users and permissions
    /// </summary>
    /// <param name="resourceType">Type of resource (project, post, content, etc.)</param>
    /// <param name="resourceId">ID of the resource to share</param>
    /// <param name="shareRequest">Details of the sharing request</param>
    /// <param name="sharedByUserId">User performing the share action</param>
    /// <returns>Result of the sharing operation</returns>
    Task<ShareResult> ShareResourceAsync(
        string resourceType,
        Guid resourceId,
        ShareResourceRequest shareRequest,
        Guid sharedByUserId);

    /// <summary>
    /// Get all users who have access to a resource and their permissions
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="requestingUserId">User requesting the information</param>
    /// <returns>List of users and their permissions</returns>
    Task<IEnumerable<ResourceUserPermission>> GetResourceUsersAsync(
        string resourceType,
        Guid resourceId,
        Guid requestingUserId);

    /// <summary>
    /// Update permissions for a user on a resource
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="targetUserId">User whose permissions to update</param>
    /// <param name="permissions">New permissions to grant</param>
    /// <param name="updatedByUserId">User performing the update</param>
    /// <param name="expiresAt">Optional expiration date</param>
    /// <returns>Result of the update operation</returns>
    Task<PermissionUpdateResult> UpdateUserPermissionsAsync(
        string resourceType,
        Guid resourceId,
        Guid targetUserId,
        PermissionType[] permissions,
        Guid updatedByUserId,
        DateTime? expiresAt = null);

    /// <summary>
    /// Remove user access from a resource
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="targetUserId">User to remove</param>
    /// <param name="removedByUserId">User performing the removal</param>
    /// <returns>Result of the removal operation</returns>
    Task<PermissionUpdateResult> RemoveUserAccessAsync(
        string resourceType,
        Guid resourceId,
        Guid targetUserId,
        Guid removedByUserId);

    /// <summary>
    /// Invite a user to access a resource via email
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="inviteRequest">Details of the invitation</param>
    /// <param name="invitedByUserId">User sending the invitation</param>
    /// <returns>Result of the invitation</returns>
    Task<InvitationResult> InviteUserToResourceAsync(
        string resourceType,
        Guid resourceId,
        InviteUserRequest inviteRequest,
        Guid invitedByUserId);

    /// <summary>
    /// Get pending invitations for a resource
    /// </summary>
    /// <param name="resourceType">Type of resource</param>
    /// <param name="resourceId">ID of the resource</param>
    /// <param name="requestingUserId">User requesting the information</param>
    /// <returns>List of pending invitations</returns>
    Task<IEnumerable<ResourceInvitation>> GetPendingInvitationsAsync(
        string resourceType,
        Guid resourceId,
        Guid requestingUserId);

    /// <summary>
    /// Accept an invitation to access a resource
    /// </summary>
    /// <param name="invitationId">ID of the invitation</param>
    /// <param name="userId">User accepting the invitation</param>
    /// <returns>Result of accepting the invitation</returns>
    Task<InvitationAcceptResult> AcceptInvitationAsync(Guid invitationId, Guid userId);

    /// <summary>
    /// Decline an invitation to access a resource
    /// </summary>
    /// <param name="invitationId">ID of the invitation</param>
    /// <param name="userId">User declining the invitation</param>
    /// <returns>Result of declining the invitation</returns>
    Task<InvitationDeclineResult> DeclineInvitationAsync(Guid invitationId, Guid userId);
}

/// <summary>
/// Request to share a resource with users
/// </summary>
public class ShareResourceRequest
{
    public string[] UserEmails { get; set; } = Array.Empty<string>();
    public Guid[] UserIds { get; set; } = Array.Empty<Guid>();
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool RequireAcceptance { get; set; } = true;
    public bool NotifyUsers { get; set; } = true;
}

/// <summary>
/// Request to invite a user to a resource
/// </summary>
public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool RequireAcceptance { get; set; } = true;
}

/// <summary>
/// Result of a sharing operation
/// </summary>
public class ShareResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<UserShareResult> UserResults { get; set; } = new();
    public Guid? ShareId { get; set; }
}

/// <summary>
/// Result for individual user in sharing operation
/// </summary>
public class UserShareResult
{
    public string? Email { get; set; }
    public Guid? UserId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool InvitationSent { get; set; }
    public Guid? InvitationId { get; set; }
}

/// <summary>
/// User and their permissions on a resource
/// </summary>
public class ResourceUserPermission
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
    public DateTime GrantedAt { get; set; }
    public Guid GrantedByUserId { get; set; }
    public string GrantedByUserName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsOwner { get; set; }
    public PermissionSource Source { get; set; }
}

/// <summary>
/// Result of updating user permissions
/// </summary>
public class PermissionUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PermissionType[] GrantedPermissions { get; set; } = Array.Empty<PermissionType>();
    public PermissionType[] RevokedPermissions { get; set; } = Array.Empty<PermissionType>();
}

/// <summary>
/// Result of an invitation operation
/// </summary>
public class InvitationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? InvitationId { get; set; }
    public bool UserExists { get; set; }
    public bool EmailSent { get; set; }
}

/// <summary>
/// Pending invitation to access a resource
/// </summary>
public class ResourceInvitation
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();
    public DateTime InvitedAt { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InvitedByUserName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public InvitationStatus Status { get; set; }
}

/// <summary>
/// Status of an invitation
/// </summary>
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
    Cancelled = 4,
}

/// <summary>
/// Result of accepting an invitation
/// </summary>
public class InvitationAcceptResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public PermissionType[] GrantedPermissions { get; set; } = Array.Empty<PermissionType>();
}

/// <summary>
/// Result of declining an invitation
/// </summary>
public class InvitationDeclineResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
