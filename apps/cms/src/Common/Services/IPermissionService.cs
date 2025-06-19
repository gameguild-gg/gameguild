using GameGuild.Common.Entities;
using GameGuild.Modules.Tenant.Models;

namespace GameGuild.Common.Services;

/// <summary>
/// Interface for the three-layer permission service
/// Layer 1: Tenant-wide permissions with default support
/// Layer 2: Content-Type permissions (to be implemented)
/// Layer 3: Resource-specific permissions (to be implemented)
/// </summary>
public interface IPermissionService
{
    // ===== LAYER 1: TENANT-WIDE PERMISSIONS =====

    /// <summary>
    /// Grant permissions to a user in a tenant, or set default permissions
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID (null for global defaults)</param>
    /// <param name="permissions">Permissions to grant</param>
    /// <returns>The tenant permission entity</returns>
    Task<TenantPermission> GrantTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions);

    /// <summary>
    /// Grant permissions to multiple users in a tenant efficiently in a single transaction
    /// </summary>
    /// <param name="userIds">User IDs to grant permissions to</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="permissions">Permissions to grant</param>
    /// <returns>List of tenant permission entities</returns>
    Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(Guid[] userIds, Guid tenantId, PermissionType[] permissions);

    /// <summary>
    /// Check if user has a specific tenant permission
    /// Resolves through hierarchy: user -> tenant default -> global default
    /// </summary>
    Task<bool> HasTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType permission);

    /// <summary>
    /// Get all permissions for a user in a tenant
    /// </summary>
    Task<IEnumerable<PermissionType>> GetTenantPermissionsAsync(Guid? userId, Guid? tenantId);

    /// <summary>
    /// Revoke specific permissions from a user in a tenant
    /// </summary>
    Task RevokeTenantPermissionAsync(Guid? userId, Guid? tenantId, PermissionType[] permissions);

    // === DEFAULT PERMISSION MANAGEMENT ===

    /// <summary>
    /// Set default permissions for a tenant
    /// </summary>
    Task<TenantPermission> SetTenantDefaultPermissionsAsync(Guid? tenantId, PermissionType[] permissions);

    /// <summary>
    /// Set global default permissions
    /// </summary>
    Task<TenantPermission> SetGlobalDefaultPermissionsAsync(PermissionType[] permissions);

    /// <summary>
    /// Get tenant default permissions
    /// </summary>
    Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid? tenantId);

    /// <summary>
    /// Get global default permissions
    /// </summary>
    Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync();

    /// <summary>
    /// Get effective permissions through hierarchy resolution
    /// </summary>
    Task<IEnumerable<PermissionType>> GetEffectiveTenantPermissionsAsync(Guid userId, Guid? tenantId);

    // === USER-TENANT MEMBERSHIP FUNCTIONALITY ===

    /// <summary>
    /// Get all tenants a user is a member of
    /// </summary>
    Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(Guid userId);

    /// <summary>
    /// Add user to a tenant
    /// </summary>
    Task<TenantPermission> JoinTenantAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Remove user from a tenant
    /// </summary>
    Task LeaveTenantAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Check if user is a member of a tenant
    /// </summary>
    Task<bool> IsUserInTenantAsync(Guid userId, Guid tenantId);

    /// <summary>
    /// Update user's membership expiration in a tenant
    /// </summary>
    Task<TenantPermission> UpdateTenantMembershipExpirationAsync(Guid userId, Guid tenantId, DateTime? expiresAt);

    // ===== LAYER 2: CONTENT-TYPE-WIDE PERMISSIONS =====

    /// <summary>
    /// Grant content-type permissions to a user in a tenant, or set default permissions
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID (null for global defaults)</param>
    /// <param name="contentTypeName">Name of the content type (e.g., "Article", "Video")</param>
    /// <param name="permissions">Permissions to grant</param>
    Task GrantContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions);

    /// <summary>
    /// Check if user has a specific content-type permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="contentTypeName">Name of the content type</param>
    /// <param name="permission">Permission to check</param>
    Task<bool> HasContentTypePermissionAsync(Guid userId, Guid? tenantId, string contentTypeName, PermissionType permission);

    /// <summary>
    /// Get all content-type permissions for a user in a tenant
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="contentTypeName">Name of the content type</param>
    Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(Guid? userId, Guid? tenantId, string contentTypeName);

    /// <summary>
    /// Revoke specific content-type permissions from a user in a tenant
    /// </summary>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="contentTypeName">Name of the content type</param>
    /// <param name="permissions">Permissions to revoke</param>
    Task RevokeContentTypePermissionAsync(Guid? userId, Guid? tenantId, string contentTypeName, PermissionType[] permissions);

    // ===== LAYER 3: RESOURCE-ENTRY PERMISSIONS =====

    /// <summary>
    /// Grant resource-specific permissions to a user
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID (null for global defaults)</param>
    /// <param name="resourceId">ID of the specific resource</param>
    /// <param name="permissions">Permissions to grant</param>
    Task GrantResourcePermissionAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : BaseEntity;

    /// <summary>
    /// Check if user has a specific resource permission
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="resourceId">ID of the specific resource</param>
    /// <param name="permission">Permission to check</param>
    Task<bool> HasResourcePermissionAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permission)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity;

    /// <summary>
    /// Get all resource permissions for a user on a specific resource
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="resourceId">ID of the specific resource</param>
    Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity;

    /// <summary>
    /// Revoke specific resource permissions from a user
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID (null for default permissions)</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="resourceId">ID of the specific resource</param>
    /// <param name="permissions">Permissions to revoke</param>
    Task RevokeResourcePermissionAsync<TPermission, TResource>(Guid? userId, Guid? tenantId, Guid resourceId, PermissionType[] permissions)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity;

    /// <summary>
    /// Get resource permissions for multiple resources at once (bulk operation)
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="resourceIds">IDs of the resources</param>
    Task<Dictionary<Guid, IEnumerable<PermissionType>>> GetBulkResourcePermissionsAsync<TPermission, TResource>(Guid userId, Guid? tenantId, Guid[] resourceIds)
        where TPermission : ResourcePermission<TResource>
        where TResource : BaseEntity;

    /// <summary>
    /// Share a resource with another user with specific permissions
    /// </summary>
    /// <typeparam name="TPermission">The resource permission type</typeparam>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="resourceId">ID of the resource to share</param>
    /// <param name="targetUserId">User to share with</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="permissions">Permissions to grant</param>
    /// <param name="expiresAt">Optional expiration date</param>
    Task ShareResourceAsync<TPermission, TResource>(Guid resourceId, Guid targetUserId, Guid? tenantId, PermissionType[] permissions, DateTime? expiresAt = null)
        where TPermission : ResourcePermission<TResource>, new()
        where TResource : BaseEntity;

    // ===== HELPER METHODS =====

    /// <summary>
    /// Get the tenant context for a specific resource
    /// </summary>
    Task<Guid?> GetResourceTenantIdAsync(Guid resourceId);

    /// <summary>
    /// Get the content type name for a specific resource
    /// </summary>
    Task<string?> GetResourceContentTypeAsync(Guid resourceId);
}
