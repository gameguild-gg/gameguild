using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Commands.UpdateUserPermissions;

/// <summary>
///     Handler for UpdateUserPermissionsCommand.
/// </summary>
public sealed class UpdateUserPermissionsCommandHandler(IResourcePermissionService resourcePermissionService, IPermissionsContext permissionsContext, ILogger<UpdateUserPermissionsCommandHandler> logger)
    : ICommandHandler<UpdateUserPermissionsCommand, PermissionUpdateResult>
{
    public async Task<PermissionUpdateResult> Handle(UpdateUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating permissions for user {TargetUserId} on resource {ResourceType}/{ResourceId}", request.TargetUserId, request.ResourceType, request.ResourceId);

        // Check if the current user has permission to manage permissions for this resource
        var resourceIdGuid = Guid.Parse(request.ResourceId);
        var hasManagePermission = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, resourceIdGuid, "Admin");

        if (!hasManagePermission)
        {
            logger.LogWarning("User {UserId} attempted to update permissions on resource {ResourceType}/{ResourceId} without Admin permission", request.UpdatedByUserId, request.ResourceType, request.ResourceId);

            return new PermissionUpdateResult { Success = false, ErrorMessage = "You don't have permission to update user permissions for this resource" };
        }

        // TODO: Validate that user can grant the requested permissions (requires IPermissionResolver.CanGrantPermissionsAsync)
        // TODO: Prevent removing owner permissions or downgrading own permissions (requires additional domain logic)

        var result = await resourcePermissionService.UpdateUserPermissionsAsync(
            request.TenantId,
            request.TargetUserId,
            request.ResourceType,
            request.ResourceId,
            request.Permissions,
            request.UpdatedByUserId,
            cancellationToken
        );

        logger.LogInformation("Permission update completed for user {TargetUserId}: {Success}", request.TargetUserId, result.Success);

        return result;
    }
}
