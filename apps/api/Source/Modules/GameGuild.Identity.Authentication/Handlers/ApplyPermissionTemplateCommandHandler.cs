using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command handler for applying a permission template to a user.
///     Grants all permissions defined in the template to the specified user.
/// </summary>
public sealed class ApplyPermissionTemplateCommandHandler(
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
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.IsActive, cancellationToken).ConfigureAwait(false);

            if (template == null)
            {
                logger.LogWarning("Permission template {TemplateId} not found or inactive", request.TemplateId);
                return ApplyPermissionTemplateResult.Failure(
                    request.UserId,
                    request.TemplateId,
                    $"Permission template {request.TemplateId} not found or inactive");
            }

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
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = grantedByUserId,
                    Reason = request.Reason ?? $"Applied from template: {template.Name}",
                    IsActive = true
                };

                dbContext.Set<TenantPermission>().Add(newPermission);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
