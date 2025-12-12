using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for managing resource permissions and invitations.
///     Handles resource sharing, user invitations, and permission management.
/// </summary>
public interface IResourcePermissionService
{
    /// <summary>
    ///     Shares a resource with multiple users by granting them permissions.
    /// </summary>
    /// <param name="request">The share request containing resource details and user IDs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure for each user.</returns>
    Task<ShareResult> ShareResourceAsync(ShareResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all users who have access to a specific resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of users with their permissions.</returns>
    Task<List<ResourceUser>> GetResourceUsersAsync(TenantId tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the permissions for a specific user on a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="newPermissions">The new set of permissions.</param>
    /// <param name="updatedByUserId">The ID of the user making the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure with permission details.</returns>
    Task<PermissionUpdateResult> UpdateUserPermissionsAsync(
        TenantId tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        string[ ] newPermissions,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Removes all access for a user on a specific resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="revokedByUserId">The ID of the user revoking access.</param>
    /// <param name="reason">Optional reason for revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if access was removed successfully.</returns>
    Task<bool> RemoveUserAccessAsync(TenantId tenantId, Guid userId, string resourceType, string resourceId, Guid revokedByUserId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invites a user to a resource via email.
    ///     Creates a pending invitation that must be accepted.
    /// </summary>
    /// <param name="request">The invitation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure with invitation details.</returns>
    Task<InvitationResult> InviteUserToResourceAsync(InviteUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all pending invitations for a specific resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending invitations.</returns>
    Task<List<ResourceInvitation>> GetPendingInvitationsAsync(TenantId tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Accepts an invitation and grants the user permissions on the resource.
    /// </summary>
    /// <param name="invitationId">The ID of the invitation.</param>
    /// <param name="acceptingUserId">The ID of the user accepting the invitation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure with permission details.</returns>
    Task<InvitationAcceptResult> AcceptInvitationAsync(Guid invitationId, Guid acceptingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Declines an invitation.
    /// </summary>
    /// <param name="invitationId">The ID of the invitation.</param>
    /// <param name="reason">Optional reason for declining.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure.</returns>
    Task<InvitationDeclineResult> DeclineInvitationAsync(Guid invitationId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all invitations sent to a specific user's email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of invitations for the user.</returns>
    Task<List<ResourceInvitation>> GetUserInvitationsAsync(string email, TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates that a resource exists and is accessible.
    /// </summary>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the resource exists and is accessible.</returns>
    Task<bool> ValidateResourceExistsAsync(string resourceType, string resourceId, TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has permission to share a specific resource.
    /// </summary>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user can share the resource.</returns>
    Task<bool> CanUserShareResourceAsync(string resourceType, string resourceId, Guid userId, TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the list of resource types supported by the permission system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of supported resource type names.</returns>
    Task<List<string>> GetSupportedResourceTypesAsync(CancellationToken cancellationToken = default);
}
