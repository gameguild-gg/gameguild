using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using GameGuild.Permissions.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Commands.GrantTenantPermission;

/// <summary>
///     Handler for GrantTenantPermissionCommand.
/// </summary>
public sealed class GrantTenantPermissionCommandHandler(IPermissionService permissionService, IPermissionsContext permissionsContext, ILogger<GrantTenantPermissionCommandHandler> logger)
    : ICommandHandler<GrantTenantPermissionCommand, Guid>
{
    public async Task<Guid> Handle(GrantTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Granting tenant permissions {Permissions} to user {UserId} in tenant {TenantId}", string.Join(", ", request.Permissions), request.UserId, request.TenantId);

        // Check if current user is tenant admin
        if (!permissionsContext.IsTenantAdmin && !permissionsContext.IsSystemAdmin)
        {
            logger.LogWarning("User {GrantedBy} attempted to grant tenant permissions without admin privileges", request.GrantedBy);

            throw new UnauthorizedAccessException("Only tenant or system administrators can grant tenant permissions");
        }

        var tenantPermission = await permissionService.GrantTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                request.GrantedBy,
                request.ExpiresAt,
                request.Reason,
                cancellationToken
            )
            .ConfigureAwait(false);

        logger.LogInformation("Successfully granted tenant permissions to user {UserId}: {PermissionId}", request.UserId, tenantPermission.Id);

        return tenantPermission.Id;
    }
}
