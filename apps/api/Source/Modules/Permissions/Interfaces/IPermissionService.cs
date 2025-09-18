namespace GameGuild;

/// <summary>
/// Domain service interface for three-layer permission management
/// Layer 1: Tenant-wide permissions with default support
/// Layer 2: Content-Type permissions
/// Layer 3: Resource-specific permissions
/// 
/// Implements the Specification pattern for complex permission queries
/// and follows Domain-Driven Design principles
/// </summary>
public interface IPermissionService {
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
  Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
      Guid[] userIds, Guid tenantId,
      PermissionType[] permissions
  );

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
  /// Get global default permissions that apply to all users
  /// </summary>
  Task<IEnumerable<PermissionType>> GetGlobalDefaultPermissionsAsync();

  /// <summary>
  /// Set global default permissions that apply to all users
  /// </summary>
  Task SetGlobalDefaultPermissionsAsync(PermissionType[] permissions);

  /// <summary>
  /// Get tenant default permissions for a specific tenant
  /// </summary>
  Task<IEnumerable<PermissionType>> GetTenantDefaultPermissionsAsync(Guid tenantId);

  // ===== LAYER 2: CONTENT-TYPE PERMISSIONS =====

  /// <summary>
  /// Grant content-type permissions to a user
  /// </summary>
  Task<ContentTypePermission> GrantContentTypePermissionAsync(
      Guid? userId, Guid? tenantId,
      string contentTypeName, PermissionType[] permissions
  );

  /// <summary>
  /// Check if user has content-type permission
  /// </summary>
  Task<bool> HasContentTypePermissionAsync(
      Guid? userId, Guid? tenantId,
      string contentTypeName, PermissionType permission
  );

  /// <summary>
  /// Get all content-type permissions for a user
  /// </summary>
  Task<IEnumerable<PermissionType>> GetContentTypePermissionsAsync(
      Guid? userId, Guid? tenantId, string contentTypeName
  );

  // ===== LAYER 3: RESOURCE-SPECIFIC PERMISSIONS =====

  /// <summary>
  /// Grant resource-specific permissions using generic resource type
  /// </summary>
  Task<TPermission> GrantResourcePermissionAsync<TPermission, TResource>(
      Guid userId, Guid? tenantId,
      Guid resourceId, PermissionType[] permissions
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;

  /// <summary>
  /// Check if user has resource permission using generic resource type
  /// </summary>
  Task<bool> HasResourcePermissionAsync<TPermission, TResource>(
      Guid userId, Guid? tenantId,
      Guid resourceId, PermissionType permission
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;

  /// <summary>
  /// Get all resource permissions for a user using generic resource type
  /// </summary>
  Task<IEnumerable<PermissionType>> GetResourcePermissionsAsync<TPermission, TResource>(
      Guid? userId, Guid? tenantId, Guid resourceId
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;

  // ===== BULK OPERATIONS =====

  /// <summary>
  /// Bulk grant resource permissions for multiple resources
  /// </summary>
  Task BulkGrantResourcePermissionAsync<TPermission, TResource>(
      Guid userId, Guid? tenantId,
      Guid[] resourceIds, PermissionType[] permissions
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;

  // ===== UTILITY METHODS =====

  /// <summary>
  /// Share a resource with another user by granting specific permissions
  /// </summary>
  Task ShareResourceAsync<TPermission, TResource>(
      Guid resourceId, Guid targetUserId, Guid? tenantId,
      PermissionType[] permissions, DateTime? expiresAt = null
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;

  /// <summary>
  /// Revoke all permissions for a user from a resource
  /// </summary>
  Task RevokeResourceAccessAsync<TPermission, TResource>(
      Guid userId, Guid? tenantId, Guid resourceId
  ) where TPermission : ResourcePermission<TResource>, new()
    where TResource : EntityBase;
}
