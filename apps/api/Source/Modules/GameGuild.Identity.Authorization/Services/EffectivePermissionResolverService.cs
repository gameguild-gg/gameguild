using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified implementation of effective permission resolver.
///     Aggregates permissions from all sources using <b>DENY-WINS</b> precedence.
/// </summary>
/// <remarks>
///     <para>
///         Permission evaluation collects allows and denies from all sources (RBAC roles,
///         tenant defaults, direct grants), then applies DENY-WINS:
///         <c>EffectivePermissions = AllowSet - DenySet</c>
///     </para>
///     <para>
///         Static permissions (system account wildcard) are protected from deny.
///     </para>
/// </remarks>
public class EffectivePermissionResolverService(
    IRbacPermissionResolver rbacResolver,
    ITenantPermissionStore tenantPermissionStore,
    IResourcePermissionStore resourcePermissionStore,
    IOptions<AuthorizationOptions> authorizationOptions,
    ILogger<EffectivePermissionResolverService> logger
) : IEffectivePermissionResolver
{
    private readonly AuthorizationOptions _authOptions = authorizationOptions.Value;
    public async Task<EffectivePermissions> ResolveAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken ct = default)
    {
        var allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allDenyPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sources = new Dictionary<string, PermissionSource>();
        var roleContributions = new List<RoleContribution>();

        // 1. Static permissions (hard-coded, non-negotiable - cannot be denied)
        var staticPerms = GetStaticPermissions(userId);
        foreach (var perm in staticPerms)
        {
            allPermissions.Add(perm);
            sources[perm] = PermissionSource.Static;
        }

        // 2. RBAC permissions (from roles, including hierarchy)
        var rbacResult = await rbacResolver.ResolvePermissionsAsync(userId, tenantId, ct).ConfigureAwait(false);
        foreach (var perm in rbacResult.Permissions)
        {
            if (allPermissions.Add(perm))
            {
                sources[perm] = PermissionSource.Role;
            }
        }
        // Collect role deny permissions
        foreach (var perm in rbacResult.DenyPermissions)
        {
            allDenyPermissions.Add(perm);
        }
        roleContributions.AddRange(rbacResult.RoleContributions);

        // 3. Tenant default permissions (if tenant context exists)
        if (tenantId.HasValue)
        {
            var tenantPermission = await tenantPermissionStore.GetPermissionAsync(tenantId.Value, ct).ConfigureAwait(false);
            if (tenantPermission != null)
            {
                foreach (var perm in tenantPermission.Permissions)
                {
                    if (allPermissions.Add(perm))
                    {
                        sources[perm] = PermissionSource.TenantDefault;
                    }
                }
                // Collect tenant deny permissions
                foreach (var perm in tenantPermission.DenyPermissions)
                {
                    allDenyPermissions.Add(perm);
                }
            }
        }

        // 4. Global default permissions
        var globalDefaults = GetGlobalDefaultPermissions();
        foreach (var perm in globalDefaults)
        {
            if (allPermissions.Add(perm))
            {
                sources[perm] = PermissionSource.GlobalDefault;
            }
        }

        // 5. Direct grants (per-resource permissions)
        if (tenantId.HasValue)
        {
            var directGrants = await resourcePermissionStore.GetUserPermissionsAsync(userId, tenantId.Value, ct).ConfigureAwait(false);
            foreach (var grant in directGrants)
            {
                foreach (var perm in grant.Permissions)
                {
                    if (allPermissions.Add(perm))
                    {
                        sources[perm] = PermissionSource.DirectGrant;
                    }
                }
            }
        }

        // 6. DENY-WINS: Remove denied permissions from effective set
        // Static permissions (wildcard for system account) are NOT subject to deny
        var effectivePermissions = new HashSet<string>(allPermissions, StringComparer.OrdinalIgnoreCase);
        foreach (var denied in allDenyPermissions)
        {
            // Don't deny static permissions (system account protection)
            if (sources.TryGetValue(denied, out var source) && source == PermissionSource.Static)
            {
                logger.LogWarning(
                    "Attempted to deny static permission {Permission} for user {UserId} - denied permissions cannot override static grants",
                    denied, userId);
                continue;
            }
            
            if (effectivePermissions.Remove(denied))
            {
                sources.Remove(denied);
                logger.LogDebug(
                    "Permission {Permission} denied for user {UserId} in tenant {TenantId}",
                    denied, userId, tenantId);
            }
        }

        logger.LogDebug(
            "Resolved {Count} effective permissions ({AllowCount} allowed, {DenyCount} denied) for user {UserId} in tenant {TenantId}",
            effectivePermissions.Count, allPermissions.Count, allDenyPermissions.Count, userId, tenantId);

        return new EffectivePermissions
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = effectivePermissions,
            Sources = sources,
            RoleContributions = roleContributions
        };
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid? tenantId,
        string permission,
        CancellationToken ct = default)
    {
        var effective = await ResolveAsync(userId, tenantId, ct).ConfigureAwait(false);
        return effective.Permissions.Contains(permission);
    }

    public async Task<bool> HasAllPermissionsAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var effective = await ResolveAsync(userId, tenantId, ct).ConfigureAwait(false);
        return permissions.All(p => effective.Permissions.Contains(p));
    }

    public async Task<bool> HasAnyPermissionAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var effective = await ResolveAsync(userId, tenantId, ct).ConfigureAwait(false);
        return permissions.Any(p => effective.Permissions.Contains(p));
    }

    /// <summary>
    ///     Hard-coded static permissions that cannot be changed at runtime.
    ///     These are typically for system accounts or super-admin users.
    /// </summary>
    private IReadOnlyList<string> GetStaticPermissions(Guid userId)
    {
        // System account with all permissions (configured via AuthorizationOptions)
        if (userId == _authOptions.SystemAccountId)
        {
            return ["*"]; // Wildcard = all permissions
        }

        return [];
    }

    /// <summary>
    ///     Global default permissions available to all authenticated users.
    /// </summary>
    private static IReadOnlyList<string> GetGlobalDefaultPermissions()
    {
        return
        [
            "profile:read",
            "profile:update",
            "notifications:read",
            "notifications:mark-read"
        ];
    }
}

/// <summary>
///     Store for tenant-level permissions.
/// </summary>
public interface ITenantPermissionStore
{
    Task<TenantPermission?> GetPermissionAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantPermission>> GetAllPermissionsAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
///     Store for resource-level permissions.
/// </summary>
public interface IResourcePermissionStore
{
    Task<IReadOnlyList<ResourceUserPermission>> GetUserPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<ResourceUserPermission>> GetResourcePermissionsAsync(
        Guid resourceId,
        CancellationToken ct = default);
}
