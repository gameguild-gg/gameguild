using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command handler for applying a permission template to a user.
///     Grants all permissions defined in the template to the specified user.
/// </summary>
public class ApplyPermissionTemplateCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<ApplyPermissionTemplateCommandHandler> logger
) : ICommandHandler<ApplyPermissionTemplateCommand, ApplyPermissionTemplateResult>
{
    public async Task<ApplyPermissionTemplateResult> Handle(
        ApplyPermissionTemplateCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Applying permission template {TemplateId} to user {UserId} in tenant {TenantId}",
            request.TemplateId, request.UserId, request.TenantId);

        try
        {
            // Get the template
            var template = await dbContext.Set<PermissionTemplate>()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive, cancellationToken);

            if (template == null)
            {
                logger.LogWarning("Permission template {TemplateId} not found or inactive", request.TemplateId);
                return ApplyPermissionTemplateResult.Failure(
                    request.UserId,
                    request.TemplateId,
                    $"Permission template {request.TemplateId} not found or inactive");
            }

            // Get existing permissions for this user in this tenant
            var tenantId = new TenantId(request.TenantId);
            var existingPermissions = await dbContext.Set<TenantPermission>()
                .Where(p => p.TenantId == tenantId &&
                            p.UserId == request.UserId &&
                            !p.IsRevoked)
                .Select(p => p.Permission)
                .ToListAsync(cancellationToken);

            // Determine which permissions to grant (excluding already granted ones)
            var permissionsToGrant = template.Permissions
                .Where(p => !existingPermissions.Contains(p))
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

            // Grant each permission from the template
            var grantedPermissions = new List<string>();
            foreach (var permission in permissionsToGrant)
            {
                var tenantPermission = new TenantPermission
                {
                    TenantId = tenantId,
                    UserId = request.UserId,
                    Permission = permission,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = request.AppliedBy ?? "System",
                    Reason = request.Reason ?? $"Applied from template: {template.Name}",
                    Source = PermissionSource.Template
                };

                dbContext.Set<TenantPermission>().Add(tenantPermission);
                grantedPermissions.Add(permission);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Applied {Count} permissions from template {TemplateId} to user {UserId}",
                grantedPermissions.Count, request.TemplateId, request.UserId);

            return ApplyPermissionTemplateResult.SuccessResult(
                request.UserId,
                request.TenantId,
                request.TemplateId,
                template.Name,
                grantedPermissions,
                request.AppliedBy);
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
}
