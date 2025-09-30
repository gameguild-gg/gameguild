namespace GameGuild;

/// <summary> Resource-specific permissions (permissions for a specific resource instance) </summary>
public class ResourcePermission<TResource> : PermissionBase where TResource : EntityBase
{
    public ResourcePermission() { }

    public ResourcePermission(Guid userId, Guid? tenantId, Guid resourceId) : base(userId, tenantId) { ResourceId = resourceId; }

    /// <summary> ID of the specific resource </summary>
    public Guid ResourceId { get; private set; }

    /// <summary> Resource type name for querying purposes </summary>
    public string ResourceType { get => typeof(TResource).Name; }

    public static ResourcePermission<TResource> Create(Guid userId, Guid? tenantId, Guid resourceId) { return new(userId, tenantId, resourceId); }
}
