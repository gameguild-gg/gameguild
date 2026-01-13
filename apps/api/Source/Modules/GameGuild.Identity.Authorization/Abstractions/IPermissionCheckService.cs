namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service interface for permission check/query operations.
///     Handles all read operations for tenant-level permissions.
/// </summary>
/// <remarks>
///     <para>
///         <b>Separation of Concerns:</b> This interface handles only read/check operations,
///         while <see cref="IPermissionGrantService"/> handles mutation operations.
///         This split follows the Command Query Responsibility Segregation (CQRS) pattern.
///     </para>
///     <para>
///         <b>Performance:</b> These methods can be cached aggressively since they don't
///         modify state. Cache invalidation is triggered by <see cref="IPermissionGrantService"/>
///         mutations via the tenant security version store.
///     </para>
/// </remarks>
public interface IPermissionCheckService
{
    /// <summary>
    ///     Checks if a user has a specific permission in a tenant.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all direct permissions for a user in a tenant.
    /// </summary>
    Task<List<string>> GetPermissionsAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets effective permissions for a user in a tenant, including defaults.
    /// </summary>
    /// <remarks>
    ///     Permissions are merged from three sources:
    ///     <list type="number">
    ///         <item>Global defaults (UserId=null, TenantId=null)</item>
    ///         <item>Tenant defaults (UserId=null, TenantId=X)</item>
    ///         <item>Direct grants (UserId=Y, TenantId=X)</item>
    ///     </list>
    /// </remarks>
    Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a user is a member of a tenant (has any permissions).
    /// </summary>
    Task<bool> IsUserInTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the global default permissions.
    /// </summary>
    Task<List<string>> GetGlobalDefaultPermissionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the default permissions for a specific tenant.
    /// </summary>
    Task<List<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
