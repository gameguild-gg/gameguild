using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Implementation of <see cref="IPermissionGrantService"/> containing grant/revoke logic.
///     This is the primary implementation - <see cref="PermissionService"/> is a backward-compatible facade.
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

        // SECURITY: Increment tenant version to invalidate all cached permissions
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

        // SECURITY: Increment tenant version to invalidate all cached permissions
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

/// <summary>
///     Implementation of <see cref="IPermissionQueryService"/> containing query/check logic.
///     This is the primary implementation - <see cref="PermissionService"/> is a backward-compatible facade.
/// </summary>
public sealed class PermissionQueryService(
    ITenantPermissionRepository repository
) : IPermissionQueryService
{
    public async Task<bool> HasTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null || existing.ExpiresAt.HasValue && existing.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return existing.HasPermission(permission);
    }

    public async Task<List<string>> GetTenantPermissionsAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null) return new List<string>();

        return existing.Permissions.ToList();
    }

    /// <summary>
    ///     Get effective permissions for a user in a tenant.
    /// </summary>
    /// <remarks>
    ///     <b>Permission Evaluation Policy: ALLOW-WINS (ADDITIVE)</b>
    ///     <para>
    ///         Permissions are merged from three sources in order:
    ///         <list type="number">
    ///             <item>Global defaults (UserId=null, TenantId=null) - system-wide baseline</item>
    ///             <item>Tenant defaults (UserId=null, TenantId=X) - tenant-specific baseline</item>
    ///             <item>Direct grants (UserId=Y, TenantId=X) - explicit user permissions</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public async Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var allPermissions = new List<string>();

        // Layer 1: Global defaults
        var globalDefaults = await GetGlobalDefaultPermissionsAsync(cancellationToken);
        allPermissions.AddRange(globalDefaults);

        // Layer 2: Tenant defaults
        if (tenantId.HasValue)
        {
            var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId.Value, cancellationToken);
            allPermissions.AddRange(tenantDefaults);
        }

        // Layer 3: Direct user permissions (excluding expired)
        var userPermissions = await repository.GetByUserAsync(userId, cancellationToken);
        var directPermissions = userPermissions
            .Where(p => p.TenantId == tenantId)
            .Where(p => !p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow)
            .SelectMany(p => p.Permissions);
        allPermissions.AddRange(directPermissions);

        return allPermissions.Distinct().ToList();
    }

    public async Task<List<string>> GetGlobalDefaultPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var defaults = await repository.GetByUserAndTenantAsync(null, null, cancellationToken);
        return defaults?.Permissions.ToList() ?? new List<string>();
    }

    public async Task<List<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var defaults = await repository.GetByUserAndTenantAsync(null, tenantId, cancellationToken);
        return defaults?.Permissions.ToList() ?? new List<string>();
    }

    public async Task<bool> IsUserInTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);
        return existing != null && (!existing.ExpiresAt.HasValue || existing.ExpiresAt.Value > DateTime.UtcNow);
    }
}

/// <summary>
///     Implementation of <see cref="IPermissionBulkService"/> containing bulk operations.
///     Delegates to <see cref="IPermissionGrantService"/> and <see cref="IPermissionQueryService"/>.
/// </summary>
public sealed class PermissionBulkService(
    IPermissionGrantService grantService,
    IPermissionQueryService queryService,
    ILogger<PermissionBulkService> logger
) : IPermissionBulkService
{
    public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
        Guid[] userIds,
        Guid tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Bulk granting permissions {Permissions} to {UserCount} users in tenant {TenantId}",
            string.Join(", ", permissions),
            userIds.Length,
            tenantId);

        var results = new List<TenantPermission>();

        foreach (var userId in userIds)
        {
            var result = await grantService.GrantTenantPermissionAsync(
                userId,
                tenantId,
                permissions,
                grantedBy,
                null,
                null,
                cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public async Task<TenantPermission> JoinTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? invitedBy = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User {UserId} joining tenant {TenantId}", userId, tenantId);

        var defaultPermissions = await queryService.GetTenantDefaultPermissionsAsync(tenantId, cancellationToken);

        return await grantService.GrantTenantPermissionAsync(
            userId,
            tenantId,
            defaultPermissions.ToArray(),
            invitedBy,
            null,
            "User joined tenant",
            cancellationToken);
    }

    public async Task<bool> LeaveTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("User {UserId} leaving tenant {TenantId}", userId, tenantId);

        var permissions = await queryService.GetTenantPermissionsAsync(userId, tenantId, cancellationToken);

        return await grantService.RevokeTenantPermissionAsync(
            userId,
            tenantId,
            permissions.ToArray(),
            cancellationToken);
    }
}
