using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Commands.RemoveUserAccess;

/// <summary>
///     Handler for RemoveUserAccessCommand.
/// </summary>
public sealed class RemoveUserAccessCommandHandler(IResourcePermissionService resourcePermissionService, IPermissionsContext permissionsContext, ILogger<RemoveUserAccessCommandHandler> logger)
    : ICommandHandler<RemoveUserAccessCommand, PermissionUpdateResult>
{
    public async Task<PermissionUpdateResult> Handle(RemoveUserAccessCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Removing user access for user {TargetUserId} from resource {ResourceType}/{ResourceId}. Reason: {Reason}",
            request.TargetUserId,
            request.ResourceType,
            request.ResourceId,
            request.Reason ?? "Not specified"
        );

        // Check if the current user has permission to manage permissions for this resource
        var resourceIdGuid = Guid.Parse(request.ResourceId);
        var hasManagePermission = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, resourceIdGuid, "Admin");

        if (!hasManagePermission)
        {
            logger.LogWarning("User {UserId} attempted to remove user access on resource {ResourceType}/{ResourceId} without Admin permission", request.RemovedByUserId, request.ResourceType, request.ResourceId);

            return new PermissionUpdateResult { Success = false, ErrorMessage = "You don't have permission to remove user access from this resource" };
        }

        // Prevent users from removing their own access (owner protection)
        if (request.TargetUserId == request.RemovedByUserId)
        {
            logger.LogWarning("User {UserId} attempted to remove their own access to resource {ResourceType}/{ResourceId}", request.RemovedByUserId, request.ResourceType, request.ResourceId);

            return new PermissionUpdateResult { Success = false, ErrorMessage = "You cannot remove your own access to a resource" };
        }

        // Check if target user is the owner (prevent removing owner)
        // Note: IsOwner checks if current user is owner, not arbitrary user
        // TODO: Need to get resource owner from resource and check if TargetUserId == OwnerId
        var isOwner = permissionsContext.IsOwner(request.TargetUserId);

        if (isOwner)
        {
            logger.LogWarning(
                "User {UserId} attempted to remove owner {TargetUserId} access to resource {ResourceType}/{ResourceId}",
                request.RemovedByUserId,
                request.TargetUserId,
                request.ResourceType,
                request.ResourceId
            );

            return new PermissionUpdateResult { Success = false, ErrorMessage = "Cannot remove resource owner's access" };
        }

        var success = await resourcePermissionService.RemoveUserAccessAsync(request.TenantId, request.TargetUserId, request.ResourceType, request.ResourceId, request.RemovedByUserId, request.Reason, cancellationToken);

        var result = new PermissionUpdateResult { Success = success, ErrorMessage = success ? null : "Failed to remove user access" };

        logger.LogInformation("Access removal completed for user {TargetUserId}: {Success}", request.TargetUserId, result.Success);

        return result;
    }
}
