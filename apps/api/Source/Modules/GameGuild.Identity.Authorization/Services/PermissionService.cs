using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Core permission service for managing tenant permissions
/// </summary>
public class PermissionService(
    ITenantPermissionRepository repository,
    IPermissionAuditService auditService,
    ILogger<PermissionService> logger
) : IPermissionService
{
    private readonly IPermissionAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<PermissionService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITenantPermissionRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<TenantPermission> GrantTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Granting permissions {Permissions} to user {UserId} in tenant {TenantId}",
            string.Join(", ", permissions),
            userId,
            tenantId
        );

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

        var result = await _repository.CreateAsync(permission, cancellationToken);

        await _auditService.LogPermissionChangeAsync(
            PermissionOperationType.Grant,
            tenantId,
            userId ?? Guid.Empty,
            grantedBy,
            null,
            null,
            null,
            null,
            string.Join(",", permissions),
            reason,
            true,
            null,
            null,
            null,
            cancellationToken
        );

        return result;
    }

    public async Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
        Guid[] userIds,
        Guid tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        CancellationToken cancellationToken = default
    )
    {
        var results = new List<TenantPermission>();

        foreach (var userId in userIds)
        {
            var result = await GrantTenantPermissionAsync(
                userId,
                tenantId,
                permissions,
                grantedBy,
                null,
                null,
                cancellationToken
            );
            results.Add(result);
        }

        return results;
    }

    public async Task<bool> RevokeTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null) return false;

        // Remove only the specified permissions
        existing.RemovePermissions(permissions);

        // If no permissions remain, delete the entity
        if (existing.Permissions.Length == 0)
        {
            await _repository.DeleteAsync(existing.Id, cancellationToken);
        }
        else
        {
            // Otherwise, update the entity with remaining permissions
            await _repository.UpdateAsync(existing, cancellationToken);
        }

        await _auditService.LogPermissionChangeAsync(
            PermissionOperationType.Revoke,
            tenantId,
            userId ?? Guid.Empty,
            null,
            null,
            null,
            null,
            string.Join(",", permissions),
            null,
            "Permissions revoked",
            true,
            null,
            null,
            null,
            cancellationToken
        );

        return true;
    }

    public async Task<bool> HasTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null || existing.ExpiresAt.HasValue && existing.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return existing.HasPermission(permission);
    }

    public async Task<List<string>> GetTenantPermissionsAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        if (existing == null) return new List<string>();

        return existing.Permissions.ToList();
    }

    /// <summary>
    /// Get effective permissions for a user in a tenant.
    /// 
    /// <para><b>Permission Evaluation Policy: ALLOW-WINS (ADDITIVE)</b></para>
    /// <para>
    /// Permissions are merged from three sources in order:
    /// <list type="number">
    ///   <item>Global defaults (UserId=null, TenantId=null) - system-wide baseline</item>
    ///   <item>Tenant defaults (UserId=null, TenantId=X) - tenant-specific baseline</item>
    ///   <item>Direct grants (UserId=Y, TenantId=X) - explicit user permissions</item>
    /// </list>
    /// </para>
    /// <para>
    /// All permissions are merged using <c>Distinct()</c> - there is no explicit deny.
    /// Expired permissions are excluded before merging.
    /// </para>
    /// <para>
    /// See: docs/security/PERMISSION_EVALUATION_POLICY.md for complete documentation.
    /// </para>
    /// </summary>
    public async Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var allPermissions = new List<string>();

        // Layer 1: Global defaults (UserId = null, TenantId = null)
        // These are system-wide baseline permissions for all users
        var globalDefaults = await GetGlobalDefaultPermissionsAsync(cancellationToken);
        allPermissions.AddRange(globalDefaults);

        // Layer 2: Tenant defaults (UserId = null, TenantId = specific tenant)
        // These are tenant-specific baseline permissions for all tenant members
        if (tenantId.HasValue)
        {
            var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId.Value, cancellationToken);
            allPermissions.AddRange(tenantDefaults);
        }

        // Layer 3: Direct user permissions
        // These are explicit grants to the specific user (excluding expired)
        var userPermissions = await _repository.GetByUserAsync(userId, cancellationToken);
        var directPermissions = userPermissions
            .Where(p => p.TenantId == tenantId)
            .Where(p => !p.ExpiresAt.HasValue || p.ExpiresAt.Value > DateTime.UtcNow)
            .SelectMany(p => p.Permissions);
        allPermissions.AddRange(directPermissions);

        // Merge: ALLOW-WINS policy - union of all permissions
        // No explicit deny support; revoking removes the grant
        return allPermissions.Distinct().ToList();
    }

    public async Task<TenantPermission> JoinTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? invitedBy = null,
        CancellationToken cancellationToken = default
    )
    {
        var defaultPermissions = await GetTenantDefaultPermissionsAsync(tenantId, cancellationToken);

        return await GrantTenantPermissionAsync(
            userId,
            tenantId,
            defaultPermissions.ToArray(),
            invitedBy,
            null,
            "User joined tenant",
            cancellationToken
        );
    }

    public async Task<bool> LeaveTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await GetTenantPermissionsAsync(userId, tenantId, cancellationToken);

        return await RevokeTenantPermissionAsync(userId, tenantId, permissions.ToArray(), cancellationToken);
    }

    public async Task<bool> IsUserInTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await _repository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        return existing != null && (!existing.ExpiresAt.HasValue || existing.ExpiresAt.Value > DateTime.UtcNow);
    }

    public async Task<List<string>> GetGlobalDefaultPermissionsAsync(CancellationToken cancellationToken = default)
    {
        // Global defaults: UserId = null, TenantId = null
        var defaults = await _repository.GetByUserAndTenantAsync(null, null, cancellationToken);

        return defaults?.Permissions.ToList() ?? new List<string>();
    }

    public async Task SetGlobalDefaultPermissionsAsync(
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Setting global default permissions: {Permissions}", string.Join(", ", permissions));

        var existing = await _repository.GetByUserAndTenantAsync(null, null, cancellationToken);

        if (existing != null)
        {
            existing.Permissions = permissions;
            await _repository.UpdateAsync(existing, cancellationToken);
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
            await _repository.CreateAsync(permission, cancellationToken);
        }
    }

    public async Task<List<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        // Tenant defaults: UserId = null, TenantId = specific tenant
        var defaults = await _repository.GetByUserAndTenantAsync(null, tenantId, cancellationToken);

        return defaults?.Permissions.ToList() ?? new List<string>();
    }

    public async Task SetTenantDefaultPermissionsAsync(
        Guid tenantId,
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Setting tenant {TenantId} default permissions: {Permissions}",
            tenantId,
            string.Join(", ", permissions)
        );

        var existing = await _repository.GetByUserAndTenantAsync(null, tenantId, cancellationToken);

        if (existing != null)
        {
            existing.Permissions = permissions;
            await _repository.UpdateAsync(existing, cancellationToken);
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
            await _repository.CreateAsync(permission, cancellationToken);
        }
    }
}
