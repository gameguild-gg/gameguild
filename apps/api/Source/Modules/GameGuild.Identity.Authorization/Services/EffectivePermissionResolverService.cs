using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified implementation of effective permission resolver.
///     Aggregates permissions from all sources using ALLOW-WINS precedence.
/// </summary>
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
        var sources = new Dictionary<string, PermissionSource>();
        var roleContributions = new List<RoleContribution>();

        // 1. Static permissions (hard-coded, non-negotiable)
        var staticPerms = GetStaticPermissions(userId);
        foreach (var perm in staticPerms)
        {
            allPermissions.Add(perm);
            sources[perm] = PermissionSource.Static;
        }

        // 2. RBAC permissions (from roles, including hierarchy)
        var rbacResult = await rbacResolver.ResolvePermissionsAsync(userId, tenantId, ct);
        foreach (var perm in rbacResult.Permissions)
        {
            if (allPermissions.Add(perm))
            {
                sources[perm] = PermissionSource.Role;
            }
        }
        roleContributions.AddRange(rbacResult.RoleContributions);

        // 3. Tenant default permissions (if tenant context exists)
        if (tenantId.HasValue)
        {
            var tenantDefaults = await GetTenantDefaultPermissionsAsync(tenantId.Value, ct);
            foreach (var perm in tenantDefaults)
            {
                if (allPermissions.Add(perm))
                {
                    sources[perm] = PermissionSource.TenantDefault;
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
            var directGrants = await resourcePermissionStore.GetUserPermissionsAsync(userId, tenantId.Value, ct);
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

        logger.LogDebug(
            "Resolved {Count} effective permissions for user {UserId} in tenant {TenantId}",
            allPermissions.Count, userId, tenantId);

        return new EffectivePermissions
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = allPermissions,
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
        var effective = await ResolveAsync(userId, tenantId, ct);
        return effective.Permissions.Contains(permission);
    }

    public async Task<bool> HasAllPermissionsAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var effective = await ResolveAsync(userId, tenantId, ct);
        return permissions.All(p => effective.Permissions.Contains(p));
    }

    public async Task<bool> HasAnyPermissionAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken ct = default)
    {
        var effective = await ResolveAsync(userId, tenantId, ct);
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

    /// <summary>
    ///     Gets default permissions for a tenant (tenant-wide grants).
    /// </summary>
    private async Task<IReadOnlyList<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken ct)
    {
        // Get tenant-wide permission grants
        var tenantPermission = await tenantPermissionStore.GetPermissionAsync(tenantId, ct);
        
        if (tenantPermission != null)
        {
            return tenantPermission.Permissions;
        }

        // Default tenant permissions
        return
        [
            "tenant:read",
            "content:read"
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
