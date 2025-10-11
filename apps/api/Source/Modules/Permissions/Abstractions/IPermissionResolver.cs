using GameGuild.Core.Domain;


namespace GameGuild.Modules.Permissions;

/// <summary> Enhanced Permission Resolver for 3-layer permission system Provides flexible permission resolution across Global, Tenant, and Resource levels </summary>
public interface IPermissionResolver
{
    /// <summary> Resolve effective permission for a user across all DAC layers </summary>
    /// <typeparam name="TResource"> The resource entity type </typeparam>
    /// <param name="userId"> User ID requesting permission </param>
    /// <param name="tenantId"> Tenant context </param>
    /// <param name="permission"> Permission to check </param>
    /// <param name="resourceId"> Optional resource ID for resource-level permissions </param>
    /// <param name="contentTypeName"> Optional content type for content-type level permissions </param>
    /// <returns> Detailed permission result with source and metadata </returns>
    Task<PermissionResult> ResolvePermissionAsync<TResource>(Guid userId, Guid? tenantId, PermissionType permission, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase;

    /// <summary> Get all effective permissions for a user in a specific context </summary>
    /// <typeparam name="TResource"> The resource entity type </typeparam>
    /// <param name="userId"> User ID </param>
    /// <param name="tenantId"> Tenant context </param>
    /// <param name="resourceId"> Optional resource ID </param>
    /// <param name="contentTypeName"> Optional content type </param>
    /// <returns> List of effective permissions with their sources </returns>
    Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync<TResource>(Guid userId, Guid? tenantId, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase;

    /// <summary> Check if a user can grant specific permissions to another user </summary>
    /// <param name="grantorUserId"> User attempting to grant permissions </param>
    /// <param name="tenantId"> Tenant context </param>
    /// <param name="permissions"> Permissions to be granted </param>
    /// <param name="resourceId"> Optional resource context </param>
    /// <param name="contentTypeName"> Optional content type context </param>
    /// <returns> True if user can grant all specified permissions </returns>
    Task<bool> CanGrantPermissionsAsync(Guid grantorUserId, Guid? tenantId, PermissionType[ ] permissions, Guid? resourceId = null, string? contentTypeName = null);

    /// <summary> Get permission hierarchy for debugging and audit purposes </summary>
    /// <typeparam name="TResource"> The resource entity type </typeparam>
    /// <param name="userId"> User ID </param>
    /// <param name="tenantId"> Tenant context </param>
    /// <param name="permission"> Permission to trace </param>
    /// <param name="resourceId"> Optional resource ID </param>
    /// <param name="contentTypeName"> Optional content type </param>
    /// <returns> Detailed permission hierarchy </returns>
    Task<PermissionHierarchy> GetPermissionHierarchyAsync<TResource>(Guid userId, Guid? tenantId, PermissionType permission, Guid? resourceId = null, string? contentTypeName = null) where TResource : EntityBase;

    /// <summary> Bulk resolve permissions for multiple resources </summary>
    /// <typeparam name="TResource"> The resource entity type </typeparam>
    /// <param name="userId"> User ID </param>
    /// <param name="tenantId"> Tenant context </param>
    /// <param name="resourceIds"> Resource IDs to check </param>
    /// <param name="permissions"> Permissions to check </param>
    /// <returns> Dictionary mapping resource IDs to permission results </returns>
    Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>> BulkResolvePermissionsAsync<TResource>(Guid userId, Guid? tenantId, Guid[ ] resourceIds, PermissionType[ ] permissions)
        where TResource : EntityBase;
}
