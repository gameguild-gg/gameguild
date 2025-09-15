using GameGuild.Modules.Permissions;

namespace GameGuild.Common;

/// <summary>
/// Interface for accessing current user's permissions and authorization context
/// Provides centralized access to permission checking and resource authorization
/// </summary>
public interface IPermissionsContext
{
    // === BASIC PERMISSION CHECKS ===

    /// <summary>
    /// Check if current user has a specific tenant permission
    /// </summary>
    /// <param name="permission">Permission type to check</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null);

    /// <summary>
    /// Check if current user has any of the specified tenant permissions
    /// </summary>
    /// <param name="permissions">Permission types to check</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <returns>True if user has any of the permissions</returns>
    Task<bool> HasAnyTenantPermissionAsync(PermissionType[] permissions, Guid? tenantId = null);

    /// <summary>
    /// Check if current user has all of the specified tenant permissions
    /// </summary>
    /// <param name="permissions">Permission types to check</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <returns>True if user has all permissions</returns>
    Task<bool> HasAllTenantPermissionsAsync(PermissionType[] permissions, Guid? tenantId = null);

    /// <summary>
    /// Get all tenant permissions for current user
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <returns>List of permission types</returns>
    Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? tenantId = null);

    // === RESOURCE-SPECIFIC PERMISSIONS ===

    /// <summary>
    /// Check if current user has permission to access a specific resource
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="permission">Permission type to check</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasResourcePermissionAsync(Guid resourceId, PermissionType permission);

    /// <summary>
    /// Check if current user has module-specific permission
    /// </summary>
    /// <param name="moduleId">Module ID</param>
    /// <param name="permission">Permission type to check</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasModulePermissionAsync(Guid moduleId, PermissionType permission);

    /// <summary>
    /// Get all resource permissions for current user
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <returns>List of permission types</returns>
    Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync(Guid resourceId);

    /// <summary>
    /// Get all module permissions for current user
    /// </summary>
    /// <param name="moduleId">Module ID</param>
    /// <returns>List of permission types</returns>
    Task<IEnumerable<PermissionType>> GetModulePermissionsAsync(Guid moduleId);

    // === ROLE-BASED CHECKS ===

    /// <summary>
    /// Check if current user has a specific role
    /// </summary>
    /// <param name="role">Role name to check</param>
    /// <returns>True if user has role</returns>
    bool HasRole(string role);

    /// <summary>
    /// Check if current user has any of the specified roles
    /// </summary>
    /// <param name="roles">Roles to check</param>
    /// <returns>True if user has any of the roles</returns>
    bool HasAnyRole(string[] roles);

    /// <summary>
    /// Get all roles for current user
    /// </summary>
    /// <returns>List of role names</returns>
    IEnumerable<string> GetRoles();

    // === CONTEXT INFORMATION ===

    /// <summary>
    /// Current user ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Whether current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether current user is a system administrator
    /// </summary>
    bool IsSystemAdmin { get; }

    /// <summary>
    /// Whether current user is a tenant administrator
    /// </summary>
    bool IsTenantAdmin { get; }

    /// <summary>
    /// Get effective permissions combining all permission sources
    /// </summary>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <returns>Dictionary of permission contexts and their permissions</returns>
    Task<Dictionary<string, IEnumerable<PermissionType>>> GetEffectivePermissionsAsync(Guid? tenantId = null);

    // === PERMISSION VALIDATION HELPERS ===

    /// <summary>
    /// Validate that current user can perform action, throw if not authorized
    /// </summary>
    /// <param name="permission">Required permission</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <param name="resourceId">Optional resource ID</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user lacks permission</exception>
    Task ValidatePermissionAsync(PermissionType permission, Guid? tenantId = null, Guid? resourceId = null);

    /// <summary>
    /// Validate that current user has any of the required permissions, throw if not authorized
    /// </summary>
    /// <param name="permissions">Required permissions (any)</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <param name="resourceId">Optional resource ID</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user lacks any permission</exception>
    Task ValidateAnyPermissionAsync(PermissionType[] permissions, Guid? tenantId = null, Guid? resourceId = null);

    /// <summary>
    /// Validate that current user has all required permissions, throw if not authorized
    /// </summary>
    /// <param name="permissions">Required permissions (all)</param>
    /// <param name="tenantId">Tenant ID (null for current tenant)</param>
    /// <param name="resourceId">Optional resource ID</param>
    /// <exception cref="UnauthorizedAccessException">Thrown if user lacks all permissions</exception>
    Task ValidateAllPermissionsAsync(PermissionType[] permissions, Guid? tenantId = null, Guid? resourceId = null);
}
