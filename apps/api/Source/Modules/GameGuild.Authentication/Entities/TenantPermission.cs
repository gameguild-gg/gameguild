using GameGuild.Authentication.Models.Permissions;

namespace GameGuild.Authentication.Entities;

/// <summary>
///     Tenant-wide permissions (Layer 1 of the 3-layer permission system)
///     Allows setting permissions at the tenant level for users or as default permissions
///     Supports both user-specific and default permission scenarios
/// </summary>
public class TenantPermission : WithPermissions
{
    /// <summary>
    ///     Default parameterless constructor (required by Entity Framework)
    /// </summary>
    public TenantPermission() { }

    /// <summary>
    ///     Constructor for creating a tenant permission
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID (null for global default permissions)</param>
    public TenantPermission(Guid? userId, Guid? tenantId) : base(userId, tenantId) { }

    /// <summary>
    ///     Check if this is a default permission (applies to all users)
    /// </summary>
    /// <returns>True if this is a default permission</returns>
    public bool IsDefaultPermission() { return !UserId.HasValue; }

    /// <summary>
    ///     Check if this is a global default permission (applies to all tenants)
    /// </summary>
    /// <returns>True if this is a global default permission</returns>
    public bool IsGlobalDefaultPermission() { return !UserId.HasValue && !TenantId.HasValue; }

    /// <summary>
    ///     Check if this is a tenant-specific default permission
    /// </summary>
    /// <returns>True if this is a tenant-specific default permission</returns>
    public bool IsTenantDefaultPermission() { return !UserId.HasValue && TenantId.HasValue; }

    /// <summary>
    ///     Check if this is a user-specific permission
    /// </summary>
    /// <returns>True if this is a user-specific permission</returns>
    public bool IsUserSpecificPermission() { return UserId.HasValue; }
}
