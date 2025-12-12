namespace GameGuild.Authentication.Models.Permissions;

/// <summary>
///     Base class for resource-specific permissions (Layer 3 of the 3-layer permission system)
///     Provides generic implementation for permissions on specific resource instances
///     Enables fine-grained access control at the individual resource level
/// </summary>
/// <typeparam name="TResource">The type of resource this permission applies to</typeparam>
public abstract class ResourcePermission<TResource> : WithPermissions where TResource : EntityBase
{
    /// <summary>
    ///     Default parameterless constructor (required by Entity Framework)
    /// </summary>
    protected ResourcePermission() { }

    /// <summary>
    ///     Constructor for creating a resource-specific permission
    /// </summary>
    /// <param name="userId">User ID who receives the permission</param>
    /// <param name="tenantId">Tenant ID (null for global permissions)</param>
    /// <param name="resourceId">ID of the specific resource</param>
    protected ResourcePermission(Guid userId, Guid? tenantId, Guid resourceId) : base(userId, tenantId)
    {
        ResourceId = resourceId;
        ResourceType = typeof(TResource).Name;
    }

    /// <summary>
    ///     ID of the specific resource this permission applies to
    /// </summary>
    public Guid ResourceId { get; set; }

    /// <summary>
    ///     Type name of the resource for easier querying and indexing
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    ///     Optional resource title or name for display purposes
    /// </summary>
    public string? ResourceTitle { get; set; }

    /// <summary>
    ///     Update the resource information
    /// </summary>
    /// <param name="resourceId">New resource ID</param>
    /// <param name="resourceTitle">Optional resource title</param>
    public void UpdateResource(Guid resourceId, string? resourceTitle = null)
    {
        ResourceId = resourceId;
        ResourceTitle = resourceTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Update the resource title
    /// </summary>
    /// <param name="resourceTitle">New resource title</param>
    public void UpdateResourceTitle(string? resourceTitle)
    {
        ResourceTitle = resourceTitle;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Check if this permission applies to a specific resource
    /// </summary>
    /// <param name="resourceId">Resource ID to check</param>
    /// <returns>True if permission applies to the resource</returns>
    public bool AppliesToResource(Guid resourceId) { return ResourceId == resourceId; }

    /// <summary>
    ///     Check if this permission is for a specific user and resource combination
    /// </summary>
    /// <param name="userId">User ID to check</param>
    /// <param name="resourceId">Resource ID to check</param>
    /// <returns>True if permission matches user and resource</returns>
    public bool IsForUserAndResource(Guid userId, Guid resourceId) { return UserId == userId && ResourceId == resourceId; }
}
