using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Queries.GetResourceUsers;

/// <summary>
///     Handler for GetResourceUsersQuery.
/// </summary>
public sealed class GetResourceUsersQueryHandler(IResourcePermissionService resourcePermissionService, IPermissionsContext permissionsContext, ILogger<GetResourceUsersQueryHandler> logger)
    : IQueryHandler<GetResourceUsersQuery, GetResourceUsersResponse>
{
    public async Task<GetResourceUsersResponse> Handle(GetResourceUsersQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        logger.LogInformation("Getting users with access to resource {ResourceType}/{ResourceId}", request.ResourceType, request.ResourceId);

        // Check if current user has permission to view users for this resource
        var resourceIdGuid = Guid.Parse(request.ResourceId);
        var canView = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, resourceIdGuid, "Read").ConfigureAwait(false);

        if (!canView && !permissionsContext.IsSystemAdmin)
        {
            logger.LogWarning("User {UserId} attempted to view users for resource {ResourceType}/{ResourceId} without permission", permissionsContext.UserId, request.ResourceType, request.ResourceId);

            throw new UnauthorizedAccessException("You don't have permission to view users for this resource");
        }

        var users = await resourcePermissionService.GetResourceUsersAsync(request.TenantId, request.ResourceType, request.ResourceId, cancellationToken).ConfigureAwait(false);

        // Filter out expired permissions if requested
        if (!request.IncludeExpired) { users = users.Where(u => !u.ExpiresAt.HasValue || u.ExpiresAt.Value > DateTime.UtcNow).ToList(); }

        logger.LogInformation("Found {UserCount} users with access to resource {ResourceType}/{ResourceId}", users.Count, request.ResourceType, request.ResourceId);

        return new GetResourceUsersResponse { ResourceType = request.ResourceType, ResourceId = request.ResourceId, Users = users };
    }
}
