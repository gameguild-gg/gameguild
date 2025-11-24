using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Commands.ShareResource;

/// <summary>
///     Handler for ShareResourceCommand.
///     Delegates to IResourcePermissionService for actual sharing logic.
/// </summary>
public sealed class ShareResourceCommandHandler(IResourcePermissionService resourcePermissionService, IPermissionsContext permissionsContext, ILogger<ShareResourceCommandHandler> logger)
    : ICommandHandler<ShareResourceCommand, ShareResult>
{
    public async Task<ShareResult> Handle(ShareResourceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sharing resource {ResourceType}/{ResourceId} with {UserCount} users by user {UserId}", request.ResourceType, request.ResourceId, request.UserIds.Length, request.GrantedByUserId);

        // Check if the current user has permission to share this resource
        var resourceIdGuid = Guid.Parse(request.ResourceId);
        var hasSharePermission = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, resourceIdGuid, "Share");

        if (!hasSharePermission)
        {
            logger.LogWarning("User {UserId} attempted to share resource {ResourceType}/{ResourceId} without Share permission", request.GrantedByUserId, request.ResourceType, request.ResourceId);

            return new ShareResult { Success = false, ErrorMessage = "You don't have permission to share this resource", UserResults = Array.Empty<UserShareResult>() };
        }

        // TODO: Validate that user can grant the requested permissions (requires IPermissionResolver.CanGrantPermissionsAsync)
        // TODO: Check for RequireAcceptance and NotifyUsers functionality (requires notification service integration)

        var shareRequest = new ShareResourceRequest
        {
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            UserIds = request.UserIds,
            Permissions = request.Permissions,
            GrantedByUserId = request.GrantedByUserId,
            ExpiresAt = request.ExpiresAt
        };

        var result = await resourcePermissionService.ShareResourceAsync(shareRequest, cancellationToken);

        logger.LogInformation("Share resource completed: {SuccessCount} succeeded, {FailureCount} failed", result.SuccessCount, result.FailureCount);

        return result;
    }
}
