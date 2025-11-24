using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Enums;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Services;

/// <summary>
///     Service for managing resource sharing and permission invitations
/// </summary>
public class ResourcePermissionService(
    IResourceUserPermissionRepository userPermissionRepository,
    IResourceInvitationRepository invitationRepository,
    ILogger<ResourcePermissionService> logger
) : IResourcePermissionService
{
    private readonly IResourceInvitationRepository _invitationRepository = invitationRepository ?? throw new ArgumentNullException(nameof(invitationRepository));

    private readonly ILogger<ResourcePermissionService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IResourceUserPermissionRepository _userPermissionRepository = userPermissionRepository ?? throw new ArgumentNullException(nameof(userPermissionRepository));

    public async Task<ShareResult> ShareResourceAsync(ShareResourceRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sharing resource {ResourceType}/{ResourceId} with {UserCount} users", request.ResourceType, request.ResourceId, request.UserIds.Length);

        // TODO: Validate resource exists using ValidateResourceExistsAsync
        // TODO: Check if current user can share using CanUserShareResourceAsync
        // TODO: Validate resource type is shareable using GetSupportedResourceTypesAsync

        var userResults = new List<UserShareResult>();

        foreach (var userId in request.UserIds)
        {
            try
            {
                var userPermission = new ResourceUserPermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    ResourceType = request.ResourceType,
                    ResourceId = request.ResourceId,
                    UserId = userId,
                    Permissions = request.Permissions,
                    GrantedByUserId = request.GrantedByUserId,
                    GrantedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt
                };

                await _userPermissionRepository.CreateAsync(userPermission, cancellationToken);

                userResults.Add(new UserShareResult { UserId = userId, Success = true, PermissionId = userPermission.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to share resource with user {UserId}", userId);
                userResults.Add(new UserShareResult { UserId = userId, Success = false, ErrorMessage = ex.Message });
            }
        }

        return new ShareResult { Success = userResults.All(r => r.Success), UserResults = userResults.ToArray() };
    }

    public async Task<List<ResourceUser>> GetResourceUsersAsync(TenantId tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default)
    {
        var userPermissions = await _userPermissionRepository.GetActiveByResourceAsync(resourceType, resourceId, tenantId.Value, cancellationToken);

        return userPermissions.Select(up => new ResourceUser
        {
            UserId = up.UserId,
            ResourceType = up.ResourceType,
            ResourceId = up.ResourceId,
            Permissions = up.Permissions,
            GrantedAt = up.GrantedAt,
            GrantedByUserId = up.GrantedByUserId,
            ExpiresAt = up.ExpiresAt,
            LastAccessedAt = up.LastAccessedAt,
            IsActive = up.IsActive
        }
            )
            .ToList();
    }

    public async Task<PermissionUpdateResult> UpdateUserPermissionsAsync(
        TenantId tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        string[] newPermissions,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Updating permissions for user {UserId} on resource {ResourceType}/{ResourceId}", userId, resourceType, resourceId);

        var existingPermission = await _userPermissionRepository.GetByUserAndResourceAsync(userId, resourceType, resourceId, tenantId.Value, cancellationToken);

        if (existingPermission == null) { return new PermissionUpdateResult { Success = false, ErrorMessage = "User does not have access to this resource" }; }

        var oldPermissions = existingPermission.Permissions;
        existingPermission.Permissions = newPermissions;

        await _userPermissionRepository.UpdateAsync(existingPermission, cancellationToken);

        return new PermissionUpdateResult { Success = true, PermissionId = existingPermission.Id, PreviousPermissions = oldPermissions, NewPermissions = newPermissions };
    }

    public async Task<bool> RemoveUserAccessAsync(TenantId tenantId, Guid userId, string resourceType, string resourceId, Guid revokedByUserId, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing access for user {UserId} from resource {ResourceType}/{ResourceId}. Reason: {Reason}", userId, resourceType, resourceId, reason);

        var existingPermission = await _userPermissionRepository.GetByUserAndResourceAsync(userId, resourceType, resourceId, tenantId.Value, cancellationToken);

        if (existingPermission == null) return false;

        return await _userPermissionRepository.DeleteAsync(existingPermission.Id, cancellationToken);
    }

    public async Task<InvitationResult> InviteUserToResourceAsync(InviteUserRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Inviting user {Email} to resource {ResourceType}/{ResourceId}", request.Email, request.ResourceType, request.ResourceId);

        // TODO: Validate email format
        // TODO: Check if user already has access
        // TODO: Send invitation email using IEmailService

        var invitation = new ResourceInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Email = request.Email,
            Permissions = request.Permissions,
            InvitedByUserId = request.InvitedByUserId,
            InvitedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(7),
            Status = InvitationStatus.Pending,
            Message = request.Message
        };

        await _invitationRepository.CreateAsync(invitation, cancellationToken);

        return new InvitationResult
        {
            Success = true,
            InvitationId = invitation.Id,
            EmailSent = false, // TODO: Update when email service is integrated
            UserExists = false // TODO: Check if user exists
        };
    }

    public async Task<List<ResourceInvitation>> GetPendingInvitationsAsync(TenantId tenantId, string resourceType, string resourceId, CancellationToken cancellationToken = default)
    {
        return await _invitationRepository.GetPendingByResourceAsync(resourceType, resourceId, tenantId.Value, cancellationToken);
    }

    public async Task<InvitationAcceptResult> AcceptInvitationAsync(Guid invitationId, Guid acceptingUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {UserId} accepting invitation {InvitationId}", acceptingUserId, invitationId);

        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);

        if (invitation == null) { return new InvitationAcceptResult { Success = false, ErrorMessage = "Invitation not found" }; }

        if (invitation.Status != InvitationStatus.Pending) { return new InvitationAcceptResult { Success = false, ErrorMessage = $"Invitation already {invitation.Status.ToString().ToLower()}" }; }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _invitationRepository.UpdateAsync(invitation, cancellationToken);

            return new InvitationAcceptResult { Success = false, ErrorMessage = "Invitation has expired" };
        }

        // Create user permission
        var userPermission = new ResourceUserPermission
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            ResourceType = invitation.ResourceType,
            ResourceId = invitation.ResourceId,
            UserId = acceptingUserId,
            Permissions = invitation.Permissions,
            GrantedByUserId = invitation.InvitedByUserId,
            GrantedAt = DateTime.UtcNow
        };

        await _userPermissionRepository.CreateAsync(userPermission, cancellationToken);

        // Update invitation status
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedByUserId = acceptingUserId;
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        return new InvitationAcceptResult { Success = true, PermissionId = userPermission.Id, InvitationId = invitation.Id };
    }

    public async Task<InvitationDeclineResult> DeclineInvitationAsync(Guid invitationId, string? reason = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Declining invitation {InvitationId}. Reason: {Reason}", invitationId, reason);

        var invitation = await _invitationRepository.GetByIdAsync(invitationId, cancellationToken);

        if (invitation is not { Status: InvitationStatus.Pending }) { return new InvitationDeclineResult { Success = false, ErrorMessage = "Invitation not found or already processed" }; }

        invitation.Status = InvitationStatus.Declined;
        invitation.AcceptedAt = DateTime.UtcNow;

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        return new InvitationDeclineResult { Success = true, InvitationId = invitationId };
    }

    public async Task<List<ResourceInvitation>> GetUserInvitationsAsync(string email, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting invitations for user {Email} in tenant {TenantId}", email, tenantId);

        var invitations = await _invitationRepository.GetPendingByEmailAsync(email, tenantId.Value, cancellationToken);

        return invitations.ToList();
    }

    public async Task<bool> ValidateResourceExistsAsync(string resourceType, string resourceId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating resource existence: {ResourceType}/{ResourceId}", resourceType, resourceId);

        // TODO: Implement resource validation logic
        // This should query the appropriate repository/service to verify the resource exists
        // For now, return true as a stub
        return await Task.FromResult(true);
    }

    public async Task<bool> CanUserShareResourceAsync(string resourceType, string resourceId, Guid userId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if user {UserId} can share resource {ResourceType}/{ResourceId}", userId, resourceType, resourceId);

        // TODO: Implement authorization check
        // This should verify:
        // 1. User has permission to share the resource
        // 2. User is owner or has sharing permission
        // 3. Tenant-level policies allow sharing
        // For now, return true as a stub
        return await Task.FromResult(true);
    }

    public async Task<List<string>> GetSupportedResourceTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving supported resource types for sharing");

        // TODO: Implement dynamic resource type discovery
        // This should return the list of resource types that support permission sharing
        // Could be configured or discovered from registered resources
        return await Task.FromResult(new List<string> { "project", "document", "folder", "dashboard", "report" });
    }
}
