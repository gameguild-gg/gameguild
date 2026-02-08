using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Context.Actors;

using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Command to grant tenant-level permissions to a user.
/// </summary>
public sealed record GrantTenantPermissionCommand : ICommand<Guid>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to grant permissions to.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the permissions to grant.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user granting the permissions.
    /// </summary>
    public required Guid GrantedBy { get; init; }

    /// <summary>
    ///     Gets the optional expiration date for the permissions.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    ///     Gets the optional reason for granting permissions.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
///     Handler for GrantTenantPermissionCommand.
/// </summary>
public sealed class GrantTenantPermissionCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<GrantTenantPermissionCommandHandler> logger)
    : ICommandHandler<GrantTenantPermissionCommand, Guid>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<Guid> Handle(GrantTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Granting tenant permissions {Permissions} to user {UserId} in tenant {TenantId}",
            string.Join(", ", request.Permissions),
            request.UserId,
            request.TenantId);

        // SECURITY: Global defaults (tenantId=null or Empty) require ManageGlobalDefaults permission
        var isGlobalDefault = request.TenantId.Value == Guid.Empty;
        if (isGlobalDefault)
        {
            if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults) && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {GrantedBy} attempted to modify global default permissions without ManageGlobalDefaults permission",
                    request.GrantedBy);

                throw new UnauthorizedAccessException(
                    "Modifying global default permissions requires 'system:manage-global-defaults' permission");
            }
        }
        else
        {
            // Check if current user is tenant admin for tenant-specific grants
            if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {GrantedBy} attempted to grant tenant permissions without admin privileges",
                    request.GrantedBy);

                throw new UnauthorizedAccessException("Only tenant or system administrators can grant tenant permissions");
            }
        }

        var tenantPermission = await grantService.GrantTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                request.GrantedBy,
                request.ExpiresAt,
                request.Reason,
                cancellationToken)
            ;

        logger.LogInformation(
            "Successfully granted tenant permissions to user {UserId}: {PermissionId}",
            request.UserId,
            tenantPermission.Id);

        return tenantPermission.Id;
    }
}

/// <summary>
///     Command to revoke tenant-level permissions from a user.
/// </summary>
public sealed record RevokeTenantPermissionCommand : ICommand<bool>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to revoke permissions from.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the permissions to revoke.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user revoking the permissions.
    /// </summary>
    public required Guid RevokedBy { get; init; }

    /// <summary>
    ///     Gets the optional reason for revoking permissions.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
///     Handler for RevokeTenantPermissionCommand.
/// </summary>
public sealed class RevokeTenantPermissionCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<RevokeTenantPermissionCommandHandler> logger)
    : ICommandHandler<RevokeTenantPermissionCommand, bool>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<bool> Handle(RevokeTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Revoking tenant permissions {Permissions} from user {UserId} in tenant {TenantId}. Reason: {Reason}",
            string.Join(", ", request.Permissions),
            request.UserId,
            request.TenantId,
            request.Reason ?? "Not specified");

        // SECURITY: Global defaults (tenantId=null or Empty) require ManageGlobalDefaults permission
        var isGlobalDefault = request.TenantId.Value == Guid.Empty;
        if (isGlobalDefault)
        {
            if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults) && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {RevokedBy} attempted to modify global default permissions without ManageGlobalDefaults permission",
                    request.RevokedBy);

                throw new UnauthorizedAccessException(
                    "Modifying global default permissions requires 'system:manage-global-defaults' permission");
            }
        }
        else
        {
            // Check if current user is tenant admin for tenant-specific revocations
            if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {RevokedBy} attempted to revoke tenant permissions without admin privileges",
                    request.RevokedBy);

                throw new UnauthorizedAccessException("Only tenant or system administrators can revoke tenant permissions");
            }
        }

        // Prevent revoking own admin permissions
        if (request.UserId == Actor.SubjectIdAsGuid &&
            (request.Permissions.Contains("TenantAdmin") || request.Permissions.Contains("Admin")))
        {
            logger.LogWarning("User {UserId} attempted to revoke their own admin permissions", request.UserId);

            throw new InvalidOperationException("Cannot revoke your own admin permissions");
        }

        var success = await grantService.RevokeTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Revoke tenant permissions completed for user {UserId}: {Success}",
            request.UserId,
            success);

        return success;
    }
}

// ========================================================================
// GLOBAL/TENANT DEFAULT PERMISSIONS COMMANDS
// ========================================================================

/// <summary>
///     Command to set global default permissions.
///     These are baseline permissions applied to all users across all tenants.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> Requires <c>system:manage-global-defaults</c> permission.</para>
/// </remarks>
public sealed record SetGlobalDefaultPermissionsCommand : ICommand<bool>
{
    /// <summary>
    ///     Gets the permissions to set as global defaults.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user setting the permissions.
    /// </summary>
    public required Guid SetBy { get; init; }
}

/// <summary>
///     Handler for SetGlobalDefaultPermissionsCommand.
/// </summary>
public sealed class SetGlobalDefaultPermissionsCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<SetGlobalDefaultPermissionsCommandHandler> logger)
    : ICommandHandler<SetGlobalDefaultPermissionsCommand, bool>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<bool> Handle(SetGlobalDefaultPermissionsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Setting global default permissions: {Permissions}",
            string.Join(", ", request.Permissions));

        // SECURITY: Global defaults require ManageGlobalDefaults permission
        if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults) && !Actor.IsSystemAdmin)
        {
            logger.LogWarning(
                "User {SetBy} attempted to set global default permissions without ManageGlobalDefaults permission",
                request.SetBy);

            throw new UnauthorizedAccessException(
                "Setting global default permissions requires 'system:manage-global-defaults' permission");
        }

        await grantService.SetGlobalDefaultPermissionsAsync(
                request.Permissions,
                request.SetBy,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Successfully set global default permissions by user {SetBy}",
            request.SetBy);

        return true;
    }
}

/// <summary>
///     Command to set tenant default permissions.
///     These are baseline permissions applied to all users in a specific tenant.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> Requires tenant admin or system admin privileges.</para>
/// </remarks>
public sealed record SetTenantDefaultPermissionsCommand : ICommand<bool>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the permissions to set as tenant defaults.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user setting the permissions.
    /// </summary>
    public required Guid SetBy { get; init; }
}

/// <summary>
///     Handler for SetTenantDefaultPermissionsCommand.
/// </summary>
public sealed class SetTenantDefaultPermissionsCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<SetTenantDefaultPermissionsCommandHandler> logger)
    : ICommandHandler<SetTenantDefaultPermissionsCommand, bool>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<bool> Handle(SetTenantDefaultPermissionsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Setting tenant {TenantId} default permissions: {Permissions}",
            request.TenantId,
            string.Join(", ", request.Permissions));

        // SECURITY: Tenant defaults require tenant admin or system admin
        if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
        {
            logger.LogWarning(
                "User {SetBy} attempted to set tenant default permissions without admin privileges",
                request.SetBy);

            throw new UnauthorizedAccessException(
                "Setting tenant default permissions requires tenant admin or system admin privileges");
        }

        await grantService.SetTenantDefaultPermissionsAsync(
                request.TenantId,
                request.Permissions,
                request.SetBy,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Successfully set tenant {TenantId} default permissions by user {SetBy}",
            request.TenantId,
            request.SetBy);

        return true;
    }
}

/// <summary>
///     Command to deny tenant-level permissions from a user.
///     Denied permissions take precedence over allowed permissions (DENY-WINS).
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> Requires tenant admin or system admin privileges.</para>
///     <para>For global defaults (tenantId=Empty), requires <c>system:manage-global-defaults</c> permission.</para>
/// </remarks>
public sealed record DenyTenantPermissionCommand : ICommand<Guid>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to deny permissions for.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the permissions to deny.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user denying the permissions.
    /// </summary>
    public required Guid DeniedBy { get; init; }

    /// <summary>
    ///     Gets the optional reason for denying permissions.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
///     Handler for DenyTenantPermissionCommand.
/// </summary>
public sealed class DenyTenantPermissionCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<DenyTenantPermissionCommandHandler> logger)
    : ICommandHandler<DenyTenantPermissionCommand, Guid>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<Guid> Handle(DenyTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Denying tenant permissions {Permissions} for user {UserId} in tenant {TenantId}. Reason: {Reason}",
            string.Join(", ", request.Permissions),
            request.UserId,
            request.TenantId,
            request.Reason ?? "Not specified");

        // SECURITY: Global defaults (tenantId=null or Empty) require ManageGlobalDefaults permission
        var isGlobalDefault = request.TenantId.Value == Guid.Empty;
        if (isGlobalDefault)
        {
            if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults) && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {DeniedBy} attempted to modify global default deny permissions without ManageGlobalDefaults permission",
                    request.DeniedBy);

                throw new UnauthorizedAccessException(
                    "Modifying global default permissions requires 'system:manage-global-defaults' permission");
            }
        }
        else
        {
            // Check if current user is tenant admin for tenant-specific denials
            if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {DeniedBy} attempted to deny tenant permissions without admin privileges",
                    request.DeniedBy);

                throw new UnauthorizedAccessException("Only tenant or system administrators can deny tenant permissions");
            }
        }

        var tenantPermission = await grantService.DenyTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                request.DeniedBy,
                request.Reason,
                cancellationToken)
            ;

        logger.LogInformation(
            "Successfully denied tenant permissions for user {UserId}: {PermissionId}",
            request.UserId,
            tenantPermission.Id);

        return tenantPermission.Id;
    }
}

/// <summary>
///     Command to remove deny entries from a user's permissions.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> Requires tenant admin or system admin privileges.</para>
///     <para>For global defaults (tenantId=Empty), requires <c>system:manage-global-defaults</c> permission.</para>
/// </remarks>
public sealed record RemoveDenyPermissionsCommand : ICommand<bool>
{
    /// <summary>
    ///     Gets the tenant ID.
    /// </summary>
    public required TenantId TenantId { get; init; }

    /// <summary>
    ///     Gets the user ID to remove deny permissions from.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Gets the deny permissions to remove.
    /// </summary>
    public required string[] Permissions { get; init; }

    /// <summary>
    ///     Gets the ID of the user removing the deny permissions.
    /// </summary>
    public required Guid RemovedBy { get; init; }
}

/// <summary>
///     Handler for RemoveDenyPermissionsCommand.
/// </summary>
public sealed class RemoveDenyPermissionsCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<RemoveDenyPermissionsCommandHandler> logger)
    : ICommandHandler<RemoveDenyPermissionsCommand, bool>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<bool> Handle(RemoveDenyPermissionsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Removing deny permissions {Permissions} from user {UserId} in tenant {TenantId}",
            string.Join(", ", request.Permissions),
            request.UserId,
            request.TenantId);

        // SECURITY: Global defaults (tenantId=null or Empty) require ManageGlobalDefaults permission
        var isGlobalDefault = request.TenantId.Value == Guid.Empty;
        if (isGlobalDefault)
        {
            if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults) && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {RemovedBy} attempted to modify global default deny permissions without ManageGlobalDefaults permission",
                    request.RemovedBy);

                throw new UnauthorizedAccessException(
                    "Modifying global default permissions requires 'system:manage-global-defaults' permission");
            }
        }
        else
        {
            // Check if current user is tenant admin for tenant-specific removals
            if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
            {
                logger.LogWarning(
                    "User {RemovedBy} attempted to remove deny permissions without admin privileges",
                    request.RemovedBy);

                throw new UnauthorizedAccessException("Only tenant or system administrators can remove deny permissions");
            }
        }

        var success = await grantService.RemoveDenyPermissionsAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Remove deny permissions completed for user {UserId}: {Success}",
            request.UserId,
            success);

        return success;
    }
}
