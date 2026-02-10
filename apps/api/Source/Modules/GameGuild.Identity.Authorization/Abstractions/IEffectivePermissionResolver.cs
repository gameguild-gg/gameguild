namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified service for resolving effective permissions (allow-focused).
///     Merges permissions from multiple sources using ALLOW-WINS policy.
/// </summary>
/// <remarks>
///     <para>
///         <b>Evaluation Policy: ALLOW-WINS (Additive)</b>
///         Permissions are merged from all sources. Having a permission in ANY source grants it.
///         Sources are evaluated in order:
///         <list type="number">
///             <item>Static/hard-coded permissions (system defaults, cannot be revoked)</item>
///             <item>Role-based permissions (RBAC - dynamic roles from database)</item>
///             <item>Global default permissions (system-wide baseline)</item>
///             <item>Tenant default permissions (tenant-specific baseline)</item>
///             <item>Direct user grants (explicit user permissions)</item>
///         </list>
///     </para>
///     <para>
///         <b>Permission Inheritance:</b>
///         Role hierarchy is respected. A user with "Admin" role inherits permissions
///         from "Manager" and "User" roles if hierarchy is configured.
///     </para>
/// </remarks>
public interface IEffectivePermissionResolver
{
    /// <summary>
    ///     Resolves all effective permissions for a user in a tenant.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID (null for global context).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The effective permissions with source tracking.</returns>
    Task<EffectivePermissions> ResolveAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has a specific permission.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission.</returns>
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has all of the specified permissions.
    /// </summary>
    Task<bool> HasAllPermissionsAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user has any of the specified permissions.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(
        Guid userId,
        Guid? tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Effective permissions for a user with source tracking.
/// </summary>
public record EffectivePermissions
{
    /// <summary>
    ///     The user ID.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    ///     The tenant ID.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     All effective permissions.
    /// </summary>
    public required IReadOnlySet<string> Permissions { get; init; }

    /// <summary>
    ///     Source of each permission for auditing.
    /// </summary>
    public required IReadOnlyDictionary<string, PermissionSource> Sources { get; init; }

    /// <summary>
    ///     Roles that contributed to the permissions.
    /// </summary>
    public IReadOnlyList<RoleContribution>? RoleContributions { get; init; }

    /// <summary>
    ///     When the permissions were resolved.
    /// </summary>
    public DateTime ResolvedAt { get; init; } = SystemClock.UtcNow;

    /// <summary>
    ///     Checks if a specific permission is present.
    /// </summary>
    public bool HasPermission(string permission)
        => Permissions.Contains(permission);

    /// <summary>
    ///     Checks if all specified permissions are present.
    /// </summary>
    public bool HasAllPermissions(IEnumerable<string> permissions)
        => permissions.All(p => Permissions.Contains(p));

    /// <summary>
    ///     Checks if any of the specified permissions are present.
    /// </summary>
    public bool HasAnyPermission(IEnumerable<string> permissions)
        => permissions.Any(p => Permissions.Contains(p));
}

/// <summary>
///     Source of a permission grant.
/// </summary>
public enum PermissionSource
{
    /// <summary>
    ///     Hard-coded system permission (cannot be revoked).
    /// </summary>
    Static = 0,

    /// <summary>
    ///     Permission from role assignment (RBAC).
    /// </summary>
    Role = 1,

    /// <summary>
    ///     Global default permission (system-wide).
    /// </summary>
    GlobalDefault = 2,

    /// <summary>
    ///     Tenant default permission.
    /// </summary>
    TenantDefault = 3,

    /// <summary>
    ///     Direct user grant.
    /// </summary>
    DirectGrant = 4,

    /// <summary>
    ///     Inherited from role hierarchy.
    /// </summary>
    RoleInheritance = 5,

    /// <summary>
    ///     Temporary/JIT elevation.
    /// </summary>
    TemporaryElevation = 6
}

/// <summary>
///     Contribution of a role to effective permissions.
/// </summary>
public record RoleContribution(
    Guid RoleId,
    string RoleName,
    IReadOnlyList<string> Permissions,
    bool IsInherited,
    Guid? InheritedFromRoleId);
