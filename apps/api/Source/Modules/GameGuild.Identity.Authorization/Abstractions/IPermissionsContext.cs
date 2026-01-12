using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Unified permission checking interface that combines user, tenant, and permission services.
///     Provides a convenient API for checking permissions in the current context.
/// </summary>
/// <remarks>
///     <para>
///         <b>MIGRATION NOTE:</b> This interface is being superseded by <see cref="IActorContextAccessor"/>
///         and <see cref="ActorContext"/> which provide pre-evaluated permissions.
///     </para>
///     <para>
///         Key differences:
///         <list type="bullet">
///             <item><see cref="ActorContext"/> contains pre-evaluated permissions loaded at request start</item>
///             <item><see cref="IPermissionsContext"/> fetches permissions on-demand from the database</item>
///         </list>
///     </para>
///     <para>
///         For simple permission checks, prefer <see cref="ActorContext.HasPermission(string)"/>.
///         For resource-level ACL checks, continue using <see cref="IPermissionsContext.HasResourcePermissionAsync"/>.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This interface is maintained for backward compatibility.")]
public interface IPermissionsContext
{
    /// <summary>
    ///     Gets the current user ID from context
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    ///     Gets the current tenant ID from context
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    ///     Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets whether the current user is a system administrator
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    ///     Gets whether the current user is a tenant administrator
    /// </summary>
    bool IsTenantAdmin { get; }

    /// <summary>
    ///     Checks if the current user has a tenant-level permission
    /// </summary>
    /// <param name="permission">The permission to check</param>
    /// <param name="tenantId">Optional tenant ID (defaults to current tenant)</param>
    /// <returns>True if user has the permission</returns>
    Task<bool> HasTenantPermissionAsync(string permission, Guid? tenantId = null);

    /// <summary>
    ///     Checks if the current user has a resource-level permission
    /// </summary>
    /// <param name="resourceType">The type of resource</param>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="permission">The permission to check</param>
    /// <returns>True if user has the permission</returns>
    Task<bool> HasResourcePermissionAsync(string resourceType, Guid resourceId, string permission);

    /// <summary>
    ///     Gets all effective permissions for the current user in the current tenant
    /// </summary>
    /// <returns>List of permission strings</returns>
    Task<List<string>> GetEffectivePermissionsAsync();

    /// <summary>
    ///     Checks if the current user is the owner of a resource
    /// </summary>
    /// <param name="resourceOwnerId">The owner user ID of the resource</param>
    /// <returns>True if current user is the owner</returns>
    bool IsOwner(Guid? resourceOwnerId);
}
