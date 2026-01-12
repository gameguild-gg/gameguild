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
    IPermissionService permissionService,
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

        // Check if current user is tenant admin
        if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
        {
            logger.LogWarning(
                "User {GrantedBy} attempted to grant tenant permissions without admin privileges",
                request.GrantedBy);

            throw new UnauthorizedAccessException("Only tenant or system administrators can grant tenant permissions");
        }

        var tenantPermission = await permissionService.GrantTenantPermissionAsync(
                request.UserId,
                request.TenantId,
                request.Permissions,
                request.GrantedBy,
                request.ExpiresAt,
                request.Reason,
                cancellationToken)
            .ConfigureAwait(false);

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
    IPermissionService permissionService,
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

        // Check if current user is tenant admin
        if (!Actor.IsTenantAdmin && !Actor.IsSystemAdmin)
        {
            logger.LogWarning(
                "User {RevokedBy} attempted to revoke tenant permissions without admin privileges",
                request.RevokedBy);

            throw new UnauthorizedAccessException("Only tenant or system administrators can revoke tenant permissions");
        }

        // Prevent revoking own admin permissions
        if (request.UserId == Actor.SubjectIdAsGuid &&
            (request.Permissions.Contains("TenantAdmin") || request.Permissions.Contains("Admin")))
        {
            logger.LogWarning("User {UserId} attempted to revoke their own admin permissions", request.UserId);

            throw new InvalidOperationException("Cannot revoke your own admin permissions");
        }

        var success = await permissionService.RevokeTenantPermissionAsync(
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
