using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Queries.HasPermission;

/// <summary>
///     Handler for HasPermissionQuery.
/// </summary>
public sealed class HasPermissionQueryHandler(IPermissionsContext permissionsContext, ILogger<HasPermissionQueryHandler> logger) : IQueryHandler<HasPermissionQuery, HasPermissionResponse>
{
    public async Task<HasPermissionResponse> Handle(HasPermissionQuery request, CancellationToken cancellationToken)
    {
        var targetUserId = request.UserId ?? permissionsContext.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        logger.LogInformation("Checking if user {UserId} has permission '{Permission}' on resource {ResourceType}/{ResourceId}", targetUserId, request.Permission, request.ResourceType, request.ResourceId);

        // Use IPermissionsContext to check permission
        var hasPermission = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, request.ResourceId, request.Permission).ConfigureAwait(false);

        var denialReason = hasPermission ? null : "User does not have the required permission on this resource";

        logger.LogInformation("Permission check result for user {UserId}: {HasPermission}", targetUserId, hasPermission);

        return new HasPermissionResponse
        {
            HasPermission = hasPermission, UserId = targetUserId, ResourceType = request.ResourceType, ResourceId = request.ResourceId, Permission = request.Permission, DenialReason = denialReason
        };
    }
}
