using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service interface for managing resource-level permissions.
///     Provides methods for sharing resources, managing user access, and checking permissions.
/// </summary>
public interface IResourcePermissionService
{
    /// <summary>
    ///     Shares a resource with a user by email.
    ///     If the user doesn't exist, creates an invitation.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="request">The share request.</param>
    /// <param name="sharedByUserId">The ID of the user sharing the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the share operation.</returns>
    Task<ShareResult> ShareResourceAsync(
        TenantId tenantId,
        string resourceType,
        string resourceId,
        ShareResourceRequest request,
        Guid sharedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a user's permissions on a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="updatedByUserId">The ID of the user making the update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the update operation.</returns>
    Task<PermissionUpdateResult> UpdateUserPermissionsAsync(
        TenantId tenantId,
        string resourceType,
        string resourceId,
        UpdatePermissionsRequest request,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes a user's access to a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="userId">The ID of the user to remove.</param>
    /// <param name="removedByUserId">The ID of the user removing access.</param>
    /// <param name="reason">Optional reason for removal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if access was removed successfully.</returns>
    Task<bool> RemoveUserAccessAsync(
        TenantId tenantId,
        string resourceType,
        string resourceId,
        Guid userId,
        Guid removedByUserId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all users with access to a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response containing all users with access.</returns>
    Task<ResourceUsersResponse> GetResourceUsersAsync(
        TenantId tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a user's effective permissions on a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user's ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's effective permissions.</returns>
    Task<string[]> GetEffectivePermissionsAsync(
        TenantId tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has a specific permission on a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user's ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission.</returns>
    Task<bool> HasPermissionAsync(
        TenantId tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Accepts a resource invitation.
    /// </summary>
    /// <param name="invitationId">The invitation ID.</param>
    /// <param name="acceptingUserId">The ID of the user accepting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the invitation was accepted successfully.</returns>
    Task<bool> AcceptInvitationAsync(
        Guid invitationId,
        Guid acceptingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Declines a resource invitation.
    /// </summary>
    /// <param name="invitationId">The invitation ID.</param>
    /// <param name="reason">Optional reason for declining.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the invitation was declined successfully.</returns>
    Task<bool> DeclineInvitationAsync(
        Guid invitationId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revokes a pending invitation.
    /// </summary>
    /// <param name="invitationId">The invitation ID.</param>
    /// <param name="revokedByUserId">The ID of the user revoking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the invitation was revoked successfully.</returns>
    Task<bool> RevokeInvitationAsync(
        Guid invitationId,
        Guid revokedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets pending invitations for a user by email.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending invitations.</returns>
    Task<IReadOnlyList<ResourceInvitation>> GetPendingInvitationsAsync(
        TenantId tenantId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all resources a user has access to.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user's ID.</param>
    /// <param name="resourceType">Optional filter by resource type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of user's resource permissions.</returns>
    Task<IReadOnlyList<ResourceUserPermission>> GetUserResourcesAsync(
        TenantId tenantId,
        Guid userId,
        string? resourceType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Bulk shares a resource with multiple users.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="requests">The list of share requests.</param>
    /// <param name="sharedByUserId">The ID of the user sharing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the bulk share operation.</returns>
    Task<BulkShareResult> BulkShareResourceAsync(
        TenantId tenantId,
        string resourceType,
        string resourceId,
        IEnumerable<ShareResourceRequest> requests,
        Guid sharedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records that a user accessed a resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user's ID.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordAccessAsync(
        TenantId tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);
}
