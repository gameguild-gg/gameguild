using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for permission queries and checks.
///     Extracted from PermissionService to follow SRP and improve testability.
/// </summary>
/// <remarks>
///     <b>Performance:</b> These methods can be cached aggressively since they don't
///     modify state. Cache invalidation is triggered by <see cref="IPermissionGrantService"/>
///     mutations via the tenant security version store.
/// </remarks>
public sealed class PermissionQueryService(
    ITenantPermissionRepository repository,
    ILogger<PermissionQueryService> logger
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
    ///     <para>
    ///         All permissions are merged using Distinct() - there is no explicit deny.
    ///         Expired permissions are excluded before merging.
    ///     </para>
    /// </remarks>
    public async Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var allPermissions = new List<string>();

        // Layer 1: Global defaults (UserId = null, TenantId = null)
        var globalDefaults = await GetGlobalDefaultPermissionsAsync(cancellationToken);
        allPermissions.AddRange(globalDefaults);

        // Layer 2: Tenant defaults (UserId = null, TenantId = specific tenant)
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

        // Merge: ALLOW-WINS policy - union of all permissions
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
