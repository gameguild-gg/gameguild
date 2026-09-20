using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for <see cref="BulkGrantTenantPermissionsCommand"/>.
///     Delegates per-user granting to the Authorization module's
///     <see cref="IPermissionBulkService"/>, which bumps tenant security versions and
///     writes audit entries for each mutation.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> bulk grants are guarded like single grants — tenant-admin
///     (within the actor's own tenant) or SystemAdmin; <see cref="Guid.Empty"/> tenant IDs
///     (global defaults) additionally require <c>system:manage-global-defaults</c>.</para>
/// </remarks>
public sealed class BulkGrantTenantPermissionsCommandHandler(
    IPermissionBulkService bulkService,
    IActorContextAccessor actorContextAccessor,
    ILogger<BulkGrantTenantPermissionsCommandHandler> logger
) : ICommandHandler<BulkGrantTenantPermissionsCommand, BulkPermissionResult>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<BulkPermissionResult> Handle(
        BulkGrantTenantPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        BulkTenantPermissionGuards.EnsureGrantAuthorized(Actor, request.TenantId, logger, request.GrantedBy);

        var permissions = ToPermissionNames(request.Permissions);
        var userIds = request.UserIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        var result = new TenantBulkPermissionResult
        {
            TotalRequested = userIds.Length,
            ProcessedAt = SystemClock.UtcNow
        };

        foreach (var userId in userIds)
        {
            try
            {
                await bulkService.BulkGrantTenantPermissionAsync(
                        [userId],
                        request.TenantId,
                        permissions,
                        Guid.TryParse(request.GrantedBy, out var grantedBy) ? grantedBy : null,
                        cancellationToken)
                    .ConfigureAwait(false);

                result.Successful++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Bulk grant failed for user {UserId} in tenant {TenantId}",
                    userId, request.TenantId);
                result.Failed++;
                result.Failures.Add(new UserBulkPermissionFailure
                {
                    UserId = userId,
                    Error = ex.GetType().Name,
                    Details = ex.Message
                });
            }
        }

        return result;
    }

    internal static string[] ToPermissionNames(IEnumerable<PermissionType> permissions) =>
        permissions.Select(p => p.ToString()).ToArray();
}

/// <summary>
///     Handler for <see cref="BulkRevokeTenantPermissionsCommand"/>.
///     Delegates per-user revocation to <see cref="IPermissionGrantService"/>, which bumps
///     tenant security versions and writes audit entries for each mutation.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> same guards as bulk grant — tenant-admin within the actor's
///     own tenant or SystemAdmin; global defaults additionally require
///     <c>system:manage-global-defaults</c>.</para>
/// </remarks>
public sealed class BulkRevokeTenantPermissionsCommandHandler(
    IPermissionGrantService grantService,
    IActorContextAccessor actorContextAccessor,
    ILogger<BulkRevokeTenantPermissionsCommandHandler> logger
) : ICommandHandler<BulkRevokeTenantPermissionsCommand, BulkPermissionResult>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<BulkPermissionResult> Handle(
        BulkRevokeTenantPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        BulkTenantPermissionGuards.EnsureRevokeAuthorized(Actor, request.TenantId, logger, request.RevokedBy);

        var permissions = BulkGrantTenantPermissionsCommandHandler.ToPermissionNames(request.Permissions);
        var userIds = request.UserIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        var result = new TenantBulkPermissionResult
        {
            TotalRequested = userIds.Length,
            ProcessedAt = SystemClock.UtcNow
        };

        foreach (var userId in userIds)
        {
            try
            {
                await grantService.RevokeTenantPermissionAsync(
                        userId,
                        request.TenantId,
                        permissions,
                        cancellationToken)
                    .ConfigureAwait(false);

                result.Successful++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Bulk revoke failed for user {UserId} in tenant {TenantId}",
                    userId, request.TenantId);
                result.Failed++;
                result.Failures.Add(new UserBulkPermissionFailure
                {
                    UserId = userId,
                    Error = ex.GetType().Name,
                    Details = ex.Message
                });
            }
        }

        return result;
    }
}

/// <summary>
///     Shared authorization guards for bulk tenant permission mutations.
///     Mirrors the guards used by the Authorization module's single-grant handlers.
/// </summary>
internal static class BulkTenantPermissionGuards
{
    public static void EnsureGrantAuthorized(ActorContext actor, Guid tenantId, ILogger logger, string? requestedBy)
    {
        EnsureAuthorized(actor, tenantId, logger, requestedBy, "grant");
    }

    public static void EnsureRevokeAuthorized(ActorContext actor, Guid tenantId, ILogger logger, string? requestedBy)
    {
        EnsureAuthorized(actor, tenantId, logger, requestedBy, "revoke");
    }

    private static void EnsureAuthorized(ActorContext actor, Guid tenantId, ILogger logger, string? requestedBy, string operation)
    {
        if (!actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (actor.IsSystemAdmin)
        {
            return;
        }

        // Global defaults (Guid.Empty tenant) affect every tenant.
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning(
                "Actor {ActorId} attempted to bulk {Operation} global default permissions without '{Permission}'",
                actor.SubjectId, operation, SystemPermission.Keys.ManageGlobalDefaults);

            throw new UnauthorizedAccessException(
                $"Bulk {operation} of global default permissions requires '{SystemPermission.Keys.ManageGlobalDefaults}' permission");
        }

        if (tenantId != actor.TenantId || !actor.IsTenantAdmin)
        {
            logger.LogWarning(
                "Actor {ActorId} attempted to bulk {Operation} permissions in tenant {TenantId} without tenant administration",
                actor.SubjectId, operation, tenantId);

            throw new UnauthorizedAccessException(
                $"Bulk {operation} of tenant permissions requires system administration or tenant administration of the target tenant");
        }
    }
}

/// <summary>
///     Concrete bulk-result carrying per-user failures for tenant permission operations.
/// </summary>
public sealed class TenantBulkPermissionResult : BulkPermissionResult;

/// <summary>
///     A per-user failure inside a bulk tenant permission operation.
/// </summary>
public sealed class UserBulkPermissionFailure : BulkPermissionFailure;
