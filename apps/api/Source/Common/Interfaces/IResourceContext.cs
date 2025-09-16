namespace GameGuild;

/// <summary>
/// Interface for accessing current resource context
/// Provides information about the resource being accessed in the current request
/// </summary>
public interface IResourceContext
{
    // === CURRENT RESOURCE INFORMATION ===

    /// <summary>
    /// Current resource ID being accessed
    /// </summary>
    Guid? ResourceId { get; }

    /// <summary>
    /// Current resource type being accessed
    /// </summary>
    string? ResourceType { get; }

    /// <summary>
    /// Current resource name/identifier
    /// </summary>
    string? ResourceName { get; }

    /// <summary>
    /// Parent resource ID (if this is a sub-resource)
    /// </summary>
    Guid? ParentResourceId { get; }

    /// <summary>
    /// Parent resource type (if this is a sub-resource)
    /// </summary>
    string? ParentResourceType { get; }

    /// <summary>
    /// Current action being performed on the resource
    /// </summary>
    string? CurrentAction { get; }

    /// <summary>
    /// Current HTTP method
    /// </summary>
    string? HttpMethod { get; }

    /// <summary>
    /// Current request path
    /// </summary>
    string? RequestPath { get; }

    /// <summary>
    /// Additional resource metadata
    /// </summary>
    IDictionary<string, object> Metadata { get; }

    // === RESOURCE HIERARCHY ===

    /// <summary>
    /// Get full resource hierarchy (from root to current)
    /// </summary>
    /// <returns>Resource hierarchy information</returns>
    IEnumerable<ResourceInfo> GetResourceHierarchy();

    /// <summary>
    /// Check if current resource is a sub-resource
    /// </summary>
    bool IsSubResource { get; }

    /// <summary>
    /// Get depth in resource hierarchy (0 = root resource)
    /// </summary>
    int HierarchyDepth { get; }

    // === RESOURCE CONTEXT MANAGEMENT ===

    /// <summary>
    /// Set current resource context
    /// </summary>
    /// <param name="resourceId">Resource ID</param>
    /// <param name="resourceType">Resource type</param>
    /// <param name="resourceName">Resource name</param>
    /// <param name="action">Current action</param>
    /// <param name="metadata">Additional metadata</param>
    void SetResourceContext(Guid? resourceId, string? resourceType, string? resourceName = null, string? action = null, IDictionary<string, object>? metadata = null);

    /// <summary>
    /// Set parent resource context
    /// </summary>
    /// <param name="parentResourceId">Parent resource ID</param>
    /// <param name="parentResourceType">Parent resource type</param>
    void SetParentResourceContext(Guid? parentResourceId, string? parentResourceType);

    /// <summary>
    /// Add metadata to current resource context
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    void AddMetadata(string key, object value);

    /// <summary>
    /// Get metadata value
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Metadata value</returns>
    T? GetMetadata<T>(string key, T? defaultValue = default);

    /// <summary>
    /// Clear current resource context
    /// </summary>
    void ClearResourceContext();

    // === HELPER METHODS ===

    /// <summary>
    /// Check if a specific resource is currently being accessed
    /// </summary>
    /// <param name="resourceId">Resource ID to check</param>
    /// <returns>True if this resource is currently being accessed</returns>
    bool IsAccessingResource(Guid resourceId);

    /// <summary>
    /// Check if a specific resource type is currently being accessed
    /// </summary>
    /// <param name="resourceType">Resource type to check</param>
    /// <returns>True if this resource type is currently being accessed</returns>
    bool IsAccessingResourceType(string resourceType);

    /// <summary>
    /// Check if a specific action is being performed
    /// </summary>
    /// <param name="action">Action to check</param>
    /// <returns>True if this action is being performed</returns>
    bool IsPerformingAction(string action);

    /// <summary>
    /// Get resource identifier for logging/auditing
    /// </summary>
    /// <returns>Resource identifier string</returns>
    string GetResourceIdentifier();
}

/// <summary>
/// Information about a resource in the hierarchy
/// </summary>
public class ResourceInfo
{
    /// <summary>
    /// Resource ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Resource type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Resource name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Hierarchy level (0 = root)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Create resource info
    /// </summary>
    public ResourceInfo(Guid? id, string? type, string? name = null, int level = 0)
    {
        Id = id;
        Type = type;
        Name = name;
        Level = level;
    }

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(Type)) parts.Add($"Type:{Type}");
        if (Id.HasValue) parts.Add($"Id:{Id}");
        if (!string.IsNullOrEmpty(Name)) parts.Add($"Name:{Name}");
        parts.Add($"Level:{Level}");

        return $"ResourceInfo({string.Join(", ", parts)})";
    }
}
