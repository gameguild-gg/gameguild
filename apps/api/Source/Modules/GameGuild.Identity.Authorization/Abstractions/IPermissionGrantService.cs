namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service interface for granting and revoking permissions.
///     Follows Single Responsibility Principle by focusing only on permission mutations.
/// </summary>
/// <remarks>
///     <para>
///         This service was extracted from <c>IPermissionService</c> to improve SRP compliance.
///         For querying permissions, use <see cref="IPermissionQueryService"/>.
///         For bulk operations, use <see cref="IPermissionBulkService"/>.
///     </para>
/// </remarks>
public interface IPermissionGrantService
{
    /// <summary>
    ///     Grant tenant-level permissions to a user.
    /// </summary>
    Task<TenantPermission> GrantTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Revoke tenant-level permissions from a user.
    /// </summary>
    Task<bool> RevokeTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Set global default permissions (applies to all users in all tenants).
    /// </summary>
    Task SetGlobalDefaultPermissionsAsync(
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Set tenant-specific default permissions (applies to all users in the tenant).
    /// </summary>
    Task SetTenantDefaultPermissionsAsync(
        Guid tenantId,
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for querying permissions.
///     Follows Single Responsibility Principle by focusing only on permission queries.
/// </summary>
/// <remarks>
///     <para>
///         This service was extracted from <c>IPermissionService</c> to improve SRP compliance.
///         For granting/revoking permissions, use <see cref="IPermissionGrantService"/>.
///     </para>
/// </remarks>
public interface IPermissionQueryService
{
    /// <summary>
    ///     Check if a user has a specific permission in a tenant.
    /// </summary>
    Task<bool> HasTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all direct permissions granted to a user in a tenant.
    /// </summary>
    Task<List<string>> GetTenantPermissionsAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get effective permissions for a user in a tenant.
    ///     Merges global defaults, tenant defaults, and direct grants.
    /// </summary>
    Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get global default permissions.
    /// </summary>
    Task<List<string>> GetGlobalDefaultPermissionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant-specific default permissions.
    /// </summary>
    Task<List<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a user is a member of a tenant (has any non-expired permissions).
    /// </summary>
    Task<bool> IsUserInTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for bulk permission operations and tenant membership.
///     Follows Single Responsibility Principle by focusing on multi-user operations.
/// </summary>
/// <remarks>
///     <para>
///         This service was extracted from <c>IPermissionService</c> to improve SRP compliance.
///         For single-user grants/revokes, use <see cref="IPermissionGrantService"/>.
///     </para>
/// </remarks>
public interface IPermissionBulkService
{
    /// <summary>
    ///     Grant permissions to multiple users at once.
    /// </summary>
    Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
        Guid[] userIds,
        Guid tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Add a user to a tenant with default permissions.
    /// </summary>
    Task<TenantPermission> JoinTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? invitedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Remove a user from a tenant (revoke all permissions).
    /// </summary>
    Task<bool> LeaveTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
