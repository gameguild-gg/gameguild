using GameGuild.Core.Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Services;

/// <summary>
/// Stub implementation of IResourcePermissionService for testing purposes
/// TODO: Implement full functionality
/// </summary>
public class ResourcePermissionService : IResourcePermissionService {
    private readonly ILogger<ResourcePermissionService> _logger;

    public ResourcePermissionService(ILogger<ResourcePermissionService> logger) {
        _logger = logger;
    }

    public Task<ShareResult> ShareResourceAsync(string resourceType, Guid resourceId, ShareResourceRequest shareRequest, Guid sharedByUserId) {
        _logger.LogWarning("ResourcePermissionService.ShareResourceAsync is not implemented - returning stub response");

        return Task.FromResult(new ShareResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented",
            ShareId = null,
            UserResults = new List<UserShareResult>()
        });
    }

    public Task<IEnumerable<ResourceUserPermission>> GetResourceUsersAsync(string resourceType, Guid resourceId, Guid requestingUserId) {
        _logger.LogWarning("ResourcePermissionService.GetResourceUsersAsync is not implemented - returning empty list");

        return Task.FromResult<IEnumerable<ResourceUserPermission>>(new List<ResourceUserPermission>());
    }

    public Task<PermissionUpdateResult> UpdateUserPermissionsAsync(string resourceType, Guid resourceId, Guid targetUserId, PermissionType[] permissions, Guid updatedByUserId, DateTime? expiresAt = null) {
        _logger.LogWarning("ResourcePermissionService.UpdateUserPermissionsAsync is not implemented - returning stub response");

        return Task.FromResult(new PermissionUpdateResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented",
            GrantedPermissions = Array.Empty<PermissionType>(),
            RevokedPermissions = Array.Empty<PermissionType>()
        });
    }

    public Task<PermissionUpdateResult> RemoveUserAccessAsync(string resourceType, Guid resourceId, Guid targetUserId, Guid removedByUserId) {
        _logger.LogWarning("ResourcePermissionService.RemoveUserAccessAsync is not implemented - returning stub response");

        return Task.FromResult(new PermissionUpdateResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented",
            GrantedPermissions = Array.Empty<PermissionType>(),
            RevokedPermissions = Array.Empty<PermissionType>()
        });
    }

    public Task<InvitationResult> InviteUserToResourceAsync(string resourceType, Guid resourceId, InviteUserRequest inviteRequest, Guid invitedByUserId) {
        _logger.LogWarning("ResourcePermissionService.InviteUserToResourceAsync is not implemented - returning stub response");

        return Task.FromResult(new InvitationResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented",
            InvitationId = null,
            UserExists = false,
            EmailSent = false
        });
    }

    public Task<IEnumerable<ResourceInvitation>> GetPendingInvitationsAsync(string resourceType, Guid resourceId, Guid requestingUserId) {
        _logger.LogWarning("ResourcePermissionService.GetPendingInvitationsAsync is not implemented - returning empty list");

        return Task.FromResult<IEnumerable<ResourceInvitation>>(new List<ResourceInvitation>());
    }

    public Task<InvitationAcceptResult> AcceptInvitationAsync(Guid invitationId, Guid acceptingUserId) {
        _logger.LogWarning("ResourcePermissionService.AcceptInvitationAsync is not implemented - returning stub response");

        return Task.FromResult(new InvitationAcceptResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented",
            GrantedPermissions = Array.Empty<PermissionType>()
        });
    }

    public Task<InvitationDeclineResult> DeclineInvitationAsync(Guid invitationId, Guid rejectingUserId) {
        _logger.LogWarning("ResourcePermissionService.DeclineInvitationAsync is not implemented - returning stub response");

        return Task.FromResult(new InvitationDeclineResult {
            Success = false,
            ErrorMessage = "ResourcePermissionService not implemented"
        });
    }





    public Task<bool> ValidateResourceExistsAsync(string resourceType, Guid resourceId) {
        _logger.LogWarning("ResourcePermissionService.ValidateResourceExistsAsync is not implemented - returning false");

        return Task.FromResult(false);
    }

    public Task<bool> CanUserShareResourceAsync(string resourceType, Guid resourceId, Guid userId) {
        _logger.LogWarning("ResourcePermissionService.CanUserShareResourceAsync is not implemented - returning false");

        return Task.FromResult(false);
    }

    public Task<IEnumerable<string>> GetSupportedResourceTypesAsync() {
        _logger.LogWarning("ResourcePermissionService.GetSupportedResourceTypesAsync is not implemented - returning empty list");

        return Task.FromResult<IEnumerable<string>>(new List<string>());
    }


}
