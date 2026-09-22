using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for <see cref="ClearPermissionCacheCommand"/>.
/// </summary>
/// <remarks>
///     <para>
///         Clears cached permission data for a tenant or a single user within a tenant.
///         The clear is two-phase:
///     </para>
///     <list type="number">
///         <item>bump the tenant security version through <see cref="ITenantSecurityVersionStore"/>
///         so every cache entry stamped with an older version is ignored; then</item>
///         <item>evict local L1 (in-process) entries through <see cref="ICacheInvalidationService"/>
///         for immediate effect on this instance.</item>
///     </list>
///     <para>
///         <b>SECURITY:</b> a cache clear is an administrative denial-of-service lever —
///         clearing without a target requires SystemAdmin; clearing a specific tenant requires
///         SystemAdmin or tenant-admin of that tenant.
///     </para>
/// </remarks>
public sealed class ClearPermissionCacheCommandHandler(
    IActorContextAccessor actorContextAccessor,
    ITenantSecurityVersionStore securityVersionStore,
    ICacheInvalidationService cacheInvalidationService,
    ILogger<ClearPermissionCacheCommandHandler> logger
) : ICommandHandler<ClearPermissionCacheCommand, bool>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<bool> Handle(ClearPermissionCacheCommand request, CancellationToken cancellationToken)
    {
        EnsureAuthorized(request);

        if (request.TenantId.HasValue)
        {
            // Bump the tenant security version: entries stamped with older versions are stale.
            var tenantKey = request.TenantId.Value.ToString();
            var newVersion = await securityVersionStore
                .IncrementVersionAsync(tenantKey, cancellationToken)
                .ConfigureAwait(false);

            // Evict local L1 entries for this tenant (and the targeted user, if any).
            if (request.UserId.HasValue)
            {
                await cacheInvalidationService
                    .InvalidateUserAsync(request.UserId.Value, request.TenantId.Value, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await cacheInvalidationService
                    .InvalidateTenantAsync(request.TenantId.Value, cancellationToken)
                    .ConfigureAwait(false);
            }

            logger.LogInformation(
                "Cleared permission caches for tenant {TenantId} (user: {UserId}); security version is now {Version}",
                request.TenantId,
                request.UserId,
                newVersion);

        }
        else if (request.UserId.HasValue)
        {
            // A user-scoped clear without a tenant cannot resolve a tenant security version;
            // fail closed rather than silently clearing nothing.
            throw new InvalidOperationException(
                "A user-scoped permission cache clear requires the tenant that owns the user's grants");
        }
        else
        {
            // Neither user nor tenant: invalidate every tenant by bumping the global version.
            const string globalKey = "global";
            var newVersion = await securityVersionStore
                .IncrementVersionAsync(globalKey, cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "Cleared global permission caches; global security version is now {Version}",
                newVersion);
        }

        return true;
    }

    /// <summary>
    ///     SECURITY: cache clears are administrative operations. Tenant-scoped clears require
    ///     SystemAdmin or tenant-admin of the target tenant; unscoped clears require SystemAdmin.
    /// </summary>
    private void EnsureAuthorized(ClearPermissionCacheCommand request)
    {
        if (!Actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (Actor.IsSystemAdmin)
        {
            return;
        }

        if (request.TenantId.HasValue && request.TenantId.Value == Actor.TenantId && Actor.IsTenantAdmin)
        {
            return;
        }

        logger.LogWarning(
            "Actor {ActorId} attempted to clear permission caches (tenant: {TenantId}, user: {UserId}) without authorization",
            Actor.SubjectId,
            request.TenantId,
            request.UserId);

        throw new UnauthorizedAccessException(
            "Clearing permission caches requires system administration or tenant administration of the target tenant");
    }
}
