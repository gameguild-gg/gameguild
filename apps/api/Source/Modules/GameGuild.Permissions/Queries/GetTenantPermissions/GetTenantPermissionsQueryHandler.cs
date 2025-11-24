using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Queries.GetTenantPermissions;

/// <summary>
///     Handler for GetTenantPermissionsQuery.
/// </summary>
public sealed class GetTenantPermissionsQueryHandler(IPermissionService permissionService, IPermissionsContext permissionsContext, ILogger<GetTenantPermissionsQueryHandler> logger)
    : IQueryHandler<GetTenantPermissionsQuery, GetTenantPermissionsResponse>
{
    public async Task<GetTenantPermissionsResponse> Handle(GetTenantPermissionsQuery request, CancellationToken cancellationToken)
    {
        var targetUserId = request.UserId ?? permissionsContext.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        logger.LogInformation("Getting tenant permissions for user {UserId} in tenant {TenantId}", targetUserId, request.TenantId);

        // Determine tenant id to operate on (request.TenantId or context tenant)
        // request.TenantId is a non-nullable TenantId struct. If it's the default (Guid.Empty),
        // fall back to the permissions context TenantId (if available).
        var tenantId = request.TenantId;

        if (tenantId.Value == Guid.Empty)
        {
            if (permissionsContext.TenantId is Guid ctxTenantId && ctxTenantId != Guid.Empty) { tenantId = ctxTenantId; }
            else { throw new InvalidOperationException("TenantId is required to retrieve tenant permissions"); }
        }

        // Get permissions based on request
        var permissions = request.IncludeEffective
            ? await permissionService.GetEffectivePermissionsAsync(targetUserId, tenantId, cancellationToken).ConfigureAwait(false)
            : await permissionService.GetTenantPermissionsAsync(targetUserId, tenantId, cancellationToken).ConfigureAwait(false);

        var isTenantAdmin = permissions.Contains("TenantAdmin") || permissions.Contains("Admin");
        var isSystemAdmin = permissions.Contains("SystemAdmin");

        logger.LogInformation("Found {PermissionCount} permissions for user {UserId} in tenant {TenantId}", permissions.Count, targetUserId, tenantId);

        return new GetTenantPermissionsResponse { UserId = targetUserId, TenantId = tenantId, Permissions = permissions, IsTenantAdmin = isTenantAdmin, IsSystemAdmin = isSystemAdmin };
    }
}
