using GameGuild.Common;
using GameGuild.Modules.Permissions;

namespace GameGuild.Common.Services;

/// <summary>
/// Enhanced DAC Permission Resolver for 3-layer permission system
/// Provides flexible permission resolution across Global, Tenant, and Resource levels
/// </summary>
public interface IDacPermissionResolver
{
    /// <summary>
    /// Resolve effective permission for a user across all DAC layers
    /// </summary>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID requesting permission</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="permission">Permission to check</param>
    /// <param name="resourceId">Optional resource ID for resource-level permissions</param>
    /// <param name="contentTypeName">Optional content type for content-type level permissions</param>
    /// <returns>Detailed permission result with source and metadata</returns>
    Task<PermissionResult> ResolvePermissionAsync<TResource>(
        Guid userId, 
        Guid? tenantId, 
        PermissionType permission, 
        Guid? resourceId = null,
        string? contentTypeName = null) where TResource : Entity;

    /// <summary>
    /// Get all effective permissions for a user in a specific context
    /// </summary>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="resourceId">Optional resource ID</param>
    /// <param name="contentTypeName">Optional content type</param>
    /// <returns>List of effective permissions with their sources</returns>
    Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync<TResource>(
        Guid userId, 
        Guid? tenantId, 
        Guid? resourceId = null,
        string? contentTypeName = null) where TResource : Entity;

    /// <summary>
    /// Check if a user can grant specific permissions to another user
    /// </summary>
    /// <param name="grantorUserId">User attempting to grant permissions</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="permissions">Permissions to be granted</param>
    /// <param name="resourceId">Optional resource context</param>
    /// <param name="contentTypeName">Optional content type context</param>
    /// <returns>True if user can grant all specified permissions</returns>
    Task<bool> CanGrantPermissionsAsync(
        Guid grantorUserId,
        Guid? tenantId,
        PermissionType[] permissions,
        Guid? resourceId = null,
        string? contentTypeName = null);

    /// <summary>
    /// Get permission hierarchy for debugging and audit purposes
    /// </summary>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="permission">Permission to trace</param>
    /// <param name="resourceId">Optional resource ID</param>
    /// <param name="contentTypeName">Optional content type</param>
    /// <returns>Detailed permission hierarchy</returns>
    Task<PermissionHierarchy> GetPermissionHierarchyAsync<TResource>(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        Guid? resourceId = null,
        string? contentTypeName = null) where TResource : Entity;

    /// <summary>
    /// Bulk resolve permissions for multiple resources
    /// </summary>
    /// <typeparam name="TResource">The resource entity type</typeparam>
    /// <param name="userId">User ID</param>
    /// <param name="tenantId">Tenant context</param>
    /// <param name="resourceIds">Resource IDs to check</param>
    /// <param name="permissions">Permissions to check</param>
    /// <returns>Dictionary mapping resource IDs to permission results</returns>
    Task<Dictionary<Guid, Dictionary<PermissionType, PermissionResult>>> BulkResolvePermissionsAsync<TResource>(
        Guid userId,
        Guid? tenantId,
        Guid[] resourceIds,
        PermissionType[] permissions) where TResource : Entity;
}

/// <summary>
/// Detailed permission resolution result
/// </summary>
public class PermissionResult
{
    public bool IsGranted { get; set; }
    public bool IsExplicitlyDenied { get; set; }
    public PermissionSource Source { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
    public int Priority { get; set; }
    public bool IsInherited { get; set; }
}

/// <summary>
/// Effective permission with all metadata
/// </summary>
public class EffectivePermission
{
    public PermissionType Permission { get; set; }
    public bool IsGranted { get; set; }
    public PermissionSource Source { get; set; }
    public string SourceDescription { get; set; } = string.Empty;
    public string? GrantedBy { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsInherited { get; set; }
    public bool IsExplicit { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Permission hierarchy for debugging and audit
/// </summary>
public class PermissionHierarchy
{
    public PermissionType Permission { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ContentTypeName { get; set; }
    public List<PermissionLayer> Layers { get; set; } = new();
    public PermissionResult FinalResult { get; set; } = new();
}

/// <summary>
/// Individual permission layer in the hierarchy
/// </summary>
public class PermissionLayer
{
    public PermissionSource Source { get; set; }
    public bool? IsGranted { get; set; }
    public bool IsDefault { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Source of permission grant/denial
/// </summary>
public enum PermissionSource
{
    None = 0,
    GlobalDefault = 1,
    TenantDefault = 2,
    ContentTypeDefault = 3,
    TenantUser = 4,
    ContentTypeUser = 5,
    ResourceDefault = 6,
    ResourceUser = 7,
    SystemOverride = 8,
}
