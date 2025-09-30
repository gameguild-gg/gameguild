namespace GameGuild.Core.Domain.Identity;

/// <summary> Context interface for permissions checking within a request scope </summary>
public interface IPermissionsContext
{
    /// <summary> Gets the current user ID from the context </summary>
    Guid? UserId { get; }

    /// <summary> Gets the current tenant ID from the context </summary>
    Guid? TenantId { get; }

    /// <summary> Indicates if the current user is authenticated </summary>
    bool IsAuthenticated { get; }

    /// <summary> Indicates if the current user is a system administrator </summary>
    bool IsSystemAdmin { get; }

    /// <summary> Indicates if the current user is a tenant administrator </summary>
    bool IsTenantAdmin { get; }

    /// <summary> Checks if the current user has a specific tenant permission </summary>
    /// <param name="permission"> The permission type to check </param>
    /// <param name="tenantId"> Optional specific tenant ID (defaults to current tenant) </param>
    /// <returns> True if the user has the permission </returns>
    Task<bool> HasTenantPermissionAsync(PermissionType permission, Guid? tenantId = null);

    /// <summary> Checks if the current user has a specific content type permission </summary>
    /// <param name="permission"> The permission type to check </param>
    /// <param name="contentType"> The content type to check </param>
    /// <param name="tenantId"> Optional specific tenant ID </param>
    /// <returns> True if the user has the permission </returns>
    Task<bool> HasContentTypePermissionAsync(PermissionType permission, string contentType, Guid? tenantId = null);

    /// <summary> Checks if the current user has a specific resource permission </summary>
    /// <param name="permission"> The permission type to check </param>
    /// <param name="resourceType"> The resource type </param>
    /// <param name="resourceId"> The resource ID </param>
    /// <param name="tenantId"> Optional specific tenant ID </param>
    /// <returns> True if the user has the permission </returns>
    Task<bool> HasResourcePermissionAsync(PermissionType permission, string resourceType, Guid resourceId, Guid? tenantId = null);

    /// <summary> Checks if the current user has any of the specified permissions </summary>
    /// <param name="permissions"> Array of permissions to check </param>
    /// <param name="tenantId"> Optional specific tenant ID </param>
    /// <returns> True if the user has any of the permissions </returns>
    Task<bool> HasAnyTenantPermissionAsync(PermissionType[ ] permissions, Guid? tenantId = null);
}
