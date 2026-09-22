using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command handler for applying a permission template to a user.
///     Grants all permissions defined in the template to the specified user.
/// </summary>
/// <remarks>
///     <para><b>SECURITY:</b> Applying a template mutates another user's permissions and is
///     therefore guarded inside the handler (defense-in-depth beyond controller attributes):</para>
///     <list type="bullet">
///         <item>the actor must be authenticated;</item>
///         <item>system templates require <c>system:manage-global-defaults</c> (or SystemAdmin);</item>
///         <item>tenant templates require tenant-admin, the <c>permissions:manage</c> permission,
///         or SystemAdmin, and the target tenant must match the actor's tenant unless the actor
///         is a SystemAdmin;</item>
///         <item>every successful apply bumps the tenant security version (cache invalidation)
///         and writes a <see cref="PermissionAuditLog"/> entry.</item>
///     </list>
/// </remarks>
public sealed class ApplyPermissionTemplateCommandHandler(
    IApplicationDbContext dbContext,
    IActorContextAccessor actorContextAccessor,
    ITenantSecurityVersionStore securityVersionStore,
    IPermissionAuditService auditService,
    ILogger<ApplyPermissionTemplateCommandHandler> logger
) : ICommandHandler<ApplyPermissionTemplateCommand, ApplyPermissionTemplateResult>
{
    private ActorContext Actor => actorContextAccessor.ActorContext;

    public async Task<ApplyPermissionTemplateResult> Handle(
        ApplyPermissionTemplateCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Applying permission template {TemplateId} to user {UserId} in tenant {TenantId}",
            request.TemplateId, request.UserId, request.TenantId);

        try
        {
            // SECURITY: the actor must be authenticated before any data is touched.
            if (!Actor.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            // SECURITY: authorization guard — applying a template grants permissions to a
            // user, so it must never run for an unauthenticated or unprivileged caller.
            var template = await dbContext.Set<PermissionTemplate>()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive, cancellationToken).ConfigureAwait(false);

            if (template == null)
            {
                logger.LogWarning("Permission template {TemplateId} not found or inactive", request.TemplateId);
                return ApplyPermissionTemplateResult.Failure(
                    request.UserId,
                    request.TemplateId,
                    $"Permission template {request.TemplateId} not found or inactive");
            }

            EnsureAuthorized(request, template);

            // Get existing TenantPermission for this user in this tenant
            var existingPermission = await dbContext.Set<TenantPermission>()
                .FirstOrDefaultAsync(p =>
                    p.TenantId == request.TenantId &&
                    p.UserId == request.UserId &&
                    p.IsActive,
                    cancellationToken).ConfigureAwait(false);

            List<string> permissionsToGrant;

            if (existingPermission != null)
            {
                // Merge template permissions with existing ones
                var existingSet = existingPermission.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
                permissionsToGrant = template.Permissions
                    .Where(p => !existingSet.Contains(p))
                    .ToList();

                if (permissionsToGrant.Count == 0)
                {
                    logger.LogInformation(
                        "User {UserId} already has all permissions from template {TemplateId}",
                        request.UserId, request.TemplateId);

                    return ApplyPermissionTemplateResult.SuccessResult(
                        request.UserId,
                        request.TenantId,
                        request.TemplateId,
                        template.Name,
                        new List<string>(),
                        request.AppliedBy);
                }

                // Update existing permission with merged set
                existingPermission.Permissions = existingPermission.Permissions
                    .Concat(permissionsToGrant)
                    .ToArray();
                existingPermission.Reason = request.Reason ?? $"Updated with template: {template.Name}";
            }
            else
            {
                // Create new permission record with all template permissions
                permissionsToGrant = template.Permissions.ToList();

                // Parse AppliedBy as Guid if possible
                Guid? grantedByUserId = null;
                if (!string.IsNullOrEmpty(request.AppliedBy) && Guid.TryParse(request.AppliedBy, out var parsedGuid))
                {
                    grantedByUserId = parsedGuid;
                }

                var newPermission = new TenantPermission
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    Permissions = template.Permissions,
                    GrantedAt = SystemClock.UtcNow,
                    GrantedBy = grantedByUserId,
                    Reason = request.Reason ?? $"Applied from template: {template.Name}",
                    IsActive = true
                };

                dbContext.Set<TenantPermission>().Add(newPermission);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // SECURITY: bump the tenant security version so every cached permission set
            // for this tenant is invalidated, and write an audit entry for the mutation.
            await InvalidateTenantCacheAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

            await auditService.LogPermissionChangeAsync(
                PermissionOperationType.Grant,
                request.UserId,
                Actor.SubjectIdAsGuid ?? Guid.Empty,
                request.TenantId,
                permissionType: string.Join(",", permissionsToGrant),
                oldValue: null,
                newValue: $"template:{template.Name}",
                reason: request.Reason ?? $"Applied template {template.Name}",
                success: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Applied {Count} permissions from template {TemplateId} to user {UserId}",
                permissionsToGrant.Count, request.TemplateId, request.UserId);

            return ApplyPermissionTemplateResult.SuccessResult(
                request.UserId,
                request.TenantId,
                request.TemplateId,
                template.Name,
                permissionsToGrant,
                request.AppliedBy);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to apply permission template {TemplateId} to user {UserId}",
                request.TemplateId, request.UserId);

            return ApplyPermissionTemplateResult.Failure(
                request.UserId,
                request.TemplateId,
                $"Failed to apply template: {ex.Message}");
        }
    }

    /// <summary>
    ///     SECURITY: validates that the current actor may apply this template.
    ///     System templates require <c>system:manage-global-defaults</c>; tenant templates
    ///     require tenant-admin or <c>permissions:manage</c>, scoped to the actor's tenant.
    /// </summary>
    private void EnsureAuthorized(ApplyPermissionTemplateCommand request, PermissionTemplate template)
    {
        if (!Actor.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        if (Actor.IsSystemAdmin)
        {
            return;
        }
        if (template.IsSystemTemplate)
        {
            if (!Actor.HasPermission(SystemPermission.Keys.ManageGlobalDefaults))
            {
                logger.LogWarning(
                    "Actor {ActorId} attempted to apply system template {TemplateId} without '{Permission}'",
                    Actor.SubjectId, request.TemplateId, SystemPermission.Keys.ManageGlobalDefaults);

                throw new UnauthorizedAccessException(
                    $"Applying system permission templates requires '{SystemPermission.Keys.ManageGlobalDefaults}' permission");
            }

            return;
        }

        // Tenant-scoped templates: the actor must operate on their own tenant and hold
        // tenant-admin or the permissions-manage permission.
        if (request.TenantId.HasValue && request.TenantId.Value != Actor.TenantId)
        {
            logger.LogWarning(
                "Actor {ActorId} attempted to apply a template in tenant {TargetTenantId} while scoped to tenant {ActorTenantId}",
                Actor.SubjectId, request.TenantId, Actor.TenantId);

            throw new UnauthorizedAccessException(
                "Applying a permission template in another tenant requires system administration privileges");
        }

        if (!Actor.IsTenantAdmin && !Actor.HasPermission(SystemPermission.Keys.ManagePermissions))
        {
            logger.LogWarning(
                "Actor {ActorId} attempted to apply template {TemplateId} without tenant-admin or '{Permission}'",
                Actor.SubjectId, request.TemplateId, SystemPermission.Keys.ManagePermissions);

            throw new UnauthorizedAccessException(
                $"Applying permission templates requires tenant administration or '{SystemPermission.Keys.ManagePermissions}' permission");
        }
    }

    private async Task InvalidateTenantCacheAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId?.ToString() ?? "global";

        var newVersion = await securityVersionStore.IncrementVersionAsync(tenantKey, cancellationToken).ConfigureAwait(false);
        logger.LogDebug(
            "Incremented security version for tenant {TenantId} to {Version} after template apply",
            tenantKey,
            newVersion);
    }
}
