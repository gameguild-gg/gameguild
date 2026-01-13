using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for permission grant/revoke mutations.
///     Extracted from PermissionService to follow SRP and improve testability.
/// </summary>
/// <remarks>
///     <b>Security:</b> All mutations increment the tenant security version to ensure
///     cache invalidation and prevent cache poisoning attacks.
/// </remarks>
public sealed class PermissionGrantService(
    ITenantPermissionRepository repository,
    IPermissionAuditService auditService,
    ITenantSecurityVersionStore securityVersionStore,
    ILogger<PermissionGrantService> logger
) : IPermissionGrantService
{
    public async Task<TenantPermission> GrantTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Granting permissions {Permissions} to user {UserId} in tenant {TenantId}",
            string.Join(", ", permissions),
            userId,
            tenantId);

        var permission = new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = permissions,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy,
            ExpiresAt = expiresAt,
            Reason = reason
        };

        var result = await repository.CreateAsync(permission, cancellationToken);

        await InvalidateTenantCacheAsync(tenantId, cancellationToken);

        await auditService.LogPermissionChangeAsync(
            PermissionOperationType.Grant,
            tenantId,
            userId ?? Guid.Empty,
            grantedBy,
            null, null, null, null,
            string.Join(",", permissions),
            reason,
            true,
            null, null, null,
            cancellationToken);

        return result;
    }

    public async Task<bool> RevokeTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null) return false;

        existing.RemovePermissions(permissions);

        if (existing.Permissions.Length == 0)
        {
            await repository.DeleteAsync(existing.Id, cancellationToken);
        }
        else
        {
            await repository.UpdateAsync(existing, cancellationToken);
        }

        await InvalidateTenantCacheAsync(tenantId, cancellationToken);

        await auditService.LogPermissionChangeAsync(
            PermissionOperationType.Revoke,
            tenantId,
            userId ?? Guid.Empty,
            null, null, null, null,
            string.Join(",", permissions),
            null,
            "Permissions revoked",
            true,
            null, null, null,
            cancellationToken);

        return true;
    }

    public async Task SetGlobalDefaultPermissionsAsync(
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting global default permissions: {Permissions}", string.Join(", ", permissions));

        var existing = await repository.GetByUserAndTenantAsync(null, null, cancellationToken);

        if (existing != null)
        {
            existing.Permissions = permissions;
            await repository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var permission = new TenantPermission
            {
                UserId = null,
                TenantId = null,
                Permissions = permissions,
                GrantedBy = setBy,
                GrantedAt = DateTime.UtcNow,
                Reason = "Global default permissions"
            };
            await repository.CreateAsync(permission, cancellationToken);
        }

        await InvalidateTenantCacheAsync(null, cancellationToken);
    }

    public async Task SetTenantDefaultPermissionsAsync(
        Guid tenantId,
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Setting tenant {TenantId} default permissions: {Permissions}",
            tenantId,
            string.Join(", ", permissions));

        var existing = await repository.GetByUserAndTenantAsync(null, tenantId, cancellationToken);

        if (existing != null)
        {
            existing.Permissions = permissions;
            await repository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var permission = new TenantPermission
            {
                UserId = null,
                TenantId = tenantId,
                Permissions = permissions,
                GrantedBy = setBy,
                GrantedAt = DateTime.UtcNow,
                Reason = "Tenant default permissions"
            };
            await repository.CreateAsync(permission, cancellationToken);
        }

        await InvalidateTenantCacheAsync(tenantId, cancellationToken);
    }

    private async Task InvalidateTenantCacheAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var tenantKey = tenantId?.ToString() ?? "global";

        try
        {
            var newVersion = await securityVersionStore.IncrementVersionAsync(tenantKey, cancellationToken);
            logger.LogDebug(
                "Incremented security version for tenant {TenantId} to {Version}",
                tenantKey,
                newVersion);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to increment security version for tenant {TenantId}. Cache may be stale.",
                tenantKey);
        }
    }
}
