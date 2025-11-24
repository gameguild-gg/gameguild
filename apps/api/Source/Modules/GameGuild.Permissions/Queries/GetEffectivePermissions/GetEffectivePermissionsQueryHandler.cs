using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Queries.GetEffectivePermissions;

/// <summary>
///     Handler for GetEffectivePermissionsQuery.
/// </summary>
public sealed class GetEffectivePermissionsQueryHandler(IPermissionsContext permissionsContext, ILogger<GetEffectivePermissionsQueryHandler> logger)
    : IQueryHandler<GetEffectivePermissionsQuery, EffectivePermissionsResponse>
{
    public async Task<EffectivePermissionsResponse> Handle(GetEffectivePermissionsQuery request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var targetUserId = request.UserId ?? permissionsContext.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        logger.LogInformation("Getting effective permissions for user {UserId} on resource {ResourceType}/{ResourceId}", targetUserId, request.ResourceType, request.ResourceId);

        // Check if current user can view permissions for this resource
        var canView = await permissionsContext.HasResourcePermissionAsync(request.ResourceType, request.ResourceId, "Read").ConfigureAwait(false);

        if (!canView && !permissionsContext.IsSystemAdmin)
        {
            logger.LogWarning("User {CurrentUserId} attempted to view permissions for resource {ResourceType}/{ResourceId} without Read permission", permissionsContext.UserId, request.ResourceType, request.ResourceId);

            throw new UnauthorizedAccessException("You don't have permission to view permissions for this resource");
        }

        // TODO: Implement actual effective permissions resolution
        // This requires integration with IPermissionResolver or IPermissionService.GetEffectivePermissionsAsync
        // For now, return a basic response structure
        var permissions = await GetEffectivePermissionsInternal(targetUserId, request.ResourceType, request.ResourceId, cancellationToken).ConfigureAwait(false);

        var isOwner = permissionsContext.IsOwner(targetUserId);

        return new EffectivePermissionsResponse
        {
            UserId = targetUserId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Permissions = permissions,
            IsOwner = isOwner,
            HasFullAccess = isOwner || permissions.Any(p => p.Permission == "Admin" || p.Permission == "Owner")
        };
    }

    private async Task<List<EffectivePermissionDto>> GetEffectivePermissionsInternal(Guid userId, string resourceType, Guid resourceId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual permission resolution logic
        // This should call IPermissionResolver.GetEffectivePermissionsAsync or similar
        // and map the results to EffectivePermissionDto

        await Task.CompletedTask; // Placeholder for async operation

        return new List<EffectivePermissionDto>();
    }
}
