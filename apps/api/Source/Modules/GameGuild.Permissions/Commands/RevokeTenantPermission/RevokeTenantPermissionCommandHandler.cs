using GameGuild.CQRS;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameGuild.Permissions.Application.Commands.RevokeTenantPermission;

/// <summary>
///     Handler for RevokeTenantPermissionCommand.
/// </summary>
public sealed class RevokeTenantPermissionCommandHandler(IPermissionService permissionService, IPermissionsContext permissionsContext, ILogger<RevokeTenantPermissionCommandHandler> logger)
    : ICommandHandler<RevokeTenantPermissionCommand, bool>
{
    public async Task<bool> Handle(RevokeTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Revoking tenant permissions {Permissions} from user {UserId} in tenant {TenantId}. Reason: {Reason}",
            string.Join(", ", request.Permissions),
            request.UserId,
            request.TenantId,
            request.Reason ?? "Not specified"
        );

        // Check if current user is tenant admin
        if (!permissionsContext.IsTenantAdmin && !permissionsContext.IsSystemAdmin)
        {
            logger.LogWarning("User {RevokedBy} attempted to revoke tenant permissions without admin privileges", request.RevokedBy);

            throw new UnauthorizedAccessException("Only tenant or system administrators can revoke tenant permissions");
        }

        // Prevent revoking own admin permissions
        if (request.UserId == permissionsContext.UserId && (request.Permissions.Contains("TenantAdmin") || request.Permissions.Contains("Admin")))
        {
            logger.LogWarning("User {UserId} attempted to revoke their own admin permissions", request.UserId);

            throw new InvalidOperationException("Cannot revoke your own admin permissions");
        }

        var success = await permissionService.RevokeTenantPermissionAsync(request.UserId, request.TenantId, request.Permissions, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Revoke tenant permissions completed for user {UserId}: {Success}", request.UserId, success);

        return success;
    }
}
