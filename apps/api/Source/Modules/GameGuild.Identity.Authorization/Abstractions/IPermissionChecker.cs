namespace GameGuild.Identity.Authorization;

/// <summary>
///     Interface for checking permissions at various levels (tenant, resource).
///     This is the "read" side of permission operations.
/// </summary>
/// <remarks>
///     <para>
///         <b>ISP Compliance:</b> This interface is segregated from <see cref="IPermissionContextInfo"/>
///         to allow clients to depend only on permission checking without needing context info.
///     </para>
///     <para>
///         <b>MIGRATION NOTE:</b> For new code, prefer using <see cref="GameGuild.Identity.Context.Actors.ActorContext.HasPermission(string)"/>
///         which provides pre-evaluated permissions without database calls.
///     </para>
/// </remarks>
public interface IPermissionChecker
{
    /// <summary>
    ///     Checks if the current user has a tenant-level permission.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "users:read", "content:write")</param>
    /// <param name="tenantId">Optional tenant ID (defaults to current tenant from context)</param>
    /// <returns>True if user has the permission, false otherwise</returns>
    Task<bool> HasTenantPermissionAsync(string permission, Guid? tenantId = null);

    /// <summary>
    ///     Checks if the current user has a resource-level permission.
    /// </summary>
    /// <param name="resourceType">The type of resource (e.g., "Project", "Document")</param>
    /// <param name="resourceId">The unique identifier of the resource</param>
    /// <param name="permission">The permission to check (e.g., "read", "write", "delete")</param>
    /// <returns>True if user has the permission on the specific resource</returns>
    Task<bool> HasResourcePermissionAsync(string resourceType, Guid resourceId, string permission);

    /// <summary>
    ///     Gets all effective permissions for the current user in the current tenant.
    /// </summary>
    /// <returns>List of permission strings the user currently has</returns>
    Task<List<string>> GetEffectivePermissionsAsync();

    /// <summary>
    ///     Checks if the current user is the owner of a resource.
    /// </summary>
    /// <param name="resourceOwnerId">The owner user ID of the resource</param>
    /// <returns>True if current user is the owner</returns>
    bool IsOwner(Guid? resourceOwnerId);
}
