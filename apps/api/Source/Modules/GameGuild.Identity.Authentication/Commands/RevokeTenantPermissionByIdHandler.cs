using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for RevokeTenantPermissionByIdCommand
/// </summary>
public sealed class RevokeTenantPermissionByIdHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    ITenantSecurityVersionStore securityVersionStore,
    IPermissionAuditService auditService)
    : ICommandHandler<RevokeTenantPermissionByIdCommand>
{
    public async Task<Unit> Handle(RevokeTenantPermissionByIdCommand request, CancellationToken cancellationToken)
    {
        var grant = await context.Set<TenantPermission>()
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant permission grant {request.GrantId} not found");

        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authenticated actor required to revoke tenant permissions");
        }

        var isGlobalDefault = !grant.TenantId.HasValue || grant.TenantId == Guid.Empty;
        if (isGlobalDefault)
        {
            if (!actor.IsSystemAdmin && !actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults))
            {
                throw new UnauthorizedAccessException(
                    "Revoking global default permissions requires 'system:manage-global-defaults' permission");
            }
        }
        else if (!actor.IsSystemAdmin && (!actor.IsTenantAdmin || actor.TenantId != grant.TenantId))
        {
            throw new UnauthorizedAccessException("Only administrators in the permission's tenant can revoke it");
        }

        var oldPermissions = grant.Permissions.ToArray();
        grant.SoftDelete();
        context.Set<TenantPermission>().Update(grant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var tenantKey = grant.TenantId?.ToString() ?? "global";
        await securityVersionStore.IncrementVersionAsync(tenantKey, cancellationToken).ConfigureAwait(false);
        await auditService.LogPermissionChangeAsync(
            PermissionOperationType.Revoke,
            grant.UserId,
            actor.SubjectIdAsGuid ?? Guid.Empty,
            grant.TenantId,
            permissionType: "Tenant",
            resourceType: "TenantPermission",
            oldValue: string.Join(",", oldPermissions),
            newValue: null,
            reason: "Tenant permission grant revoked by id",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
