namespace GameGuild.Modules.Resources;

/// <summary> Service for managing resource-level permission sharing and collaboration </summary>
public interface IResourcePermissionService
{
    /// <summary> Share a resource with specific users and permissions </summary>
    /// <param name="resourceType"> Type of resource (project, post, content, etc.) </param>
    /// <param name="resourceId"> ID of the resource to share </param>
    /// <param name="shareRequest"> Details of the sharing request </param>
    /// <param name="sharedByUserId"> User performing the share action </param>
    /// <returns> Result of the sharing operation </returns>
    Task<ShareResult> ShareResourceAsync(string resourceType, Guid resourceId, ShareResourceRequest shareRequest, Guid sharedByUserId);

    /// <summary> Get all users who have access to a resource and their permissions </summary>
    /// <param name="resourceType"> Type of resource </param>
    /// <param name="resourceId"> ID of the resource </param>
    /// <param name="requestingUserId"> User requesting the information </param>
    /// <returns> List of users and their permissions </returns>
    Task<IEnumerable<ResourceUserPermission>> GetResourceUsersAsync(string resourceType, Guid resourceId, Guid requestingUserId);

    /// <summary> Update permissions for a user on a resource </summary>
    /// <param name="resourceType"> Type of resource </param>
    /// <param name="resourceId"> ID of the resource </param>
    /// <param name="targetUserId"> User whose permissions to update </param>
    /// <param name="permissions"> New permissions to grant </param>
    /// <param name="updatedByUserId"> User performing the update </param>
    /// <param name="expiresAt"> Optional expiration date </param>
    /// <returns> Result of the update operation </returns>
    Task<PermissionUpdateResult> UpdateUserPermissionsAsync(string resourceType, Guid resourceId, Guid targetUserId, PermissionType[ ] permissions, Guid updatedByUserId, DateTime? expiresAt = null);

    /// <summary> Remove user access from a resource </summary>
    /// <param name="resourceType"> Type of resource </param>
    /// <param name="resourceId"> ID of the resource </param>
    /// <param name="targetUserId"> User to remove </param>
    /// <param name="removedByUserId"> User performing the removal </param>
    /// <returns> Result of the removal operation </returns>
    Task<PermissionUpdateResult> RemoveUserAccessAsync(string resourceType, Guid resourceId, Guid targetUserId, Guid removedByUserId);

    /// <summary> Invite a user to access a resource via email </summary>
    /// <param name="resourceType"> Type of resource </param>
    /// <param name="resourceId"> ID of the resource </param>
    /// <param name="inviteRequest"> Details of the invitation </param>
    /// <param name="invitedByUserId"> User sending the invitation </param>
    /// <returns> Result of the invitation </returns>
    Task<InvitationResult> InviteUserToResourceAsync(string resourceType, Guid resourceId, InviteUserRequest inviteRequest, Guid invitedByUserId);

    /// <summary> Get pending invitations for a resource </summary>
    /// <param name="resourceType"> Type of resource </param>
    /// <param name="resourceId"> ID of the resource </param>
    /// <param name="requestingUserId"> User requesting the information </param>
    /// <returns> List of pending invitations </returns>
    Task<IEnumerable<ResourceInvitation>> GetPendingInvitationsAsync(string resourceType, Guid resourceId, Guid requestingUserId);

    /// <summary> Accept an invitation to access a resource </summary>
    /// <param name="invitationId"> ID of the invitation </param>
    /// <param name="userId"> User accepting the invitation </param>
    /// <returns> Result of accepting the invitation </returns>
    Task<InvitationAcceptResult> AcceptInvitationAsync(Guid invitationId, Guid userId);

    /// <summary> Decline an invitation to access a resource </summary>
    /// <param name="invitationId"> ID of the invitation </param>
    /// <param name="userId"> User declining the invitation </param>
    /// <returns> Result of declining the invitation </returns>
    Task<InvitationDeclineResult> DeclineInvitationAsync(Guid invitationId, Guid userId);
}
