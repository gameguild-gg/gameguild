using GameGuild.Identity.Authorization;
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Context for ABAC policy evaluation containing all necessary attributes
/// </summary>
public class AbacEvaluationContext
{
    /// <summary>
    ///     User attributes for policy evaluation
    /// </summary>
    public Dictionary<string, object> UserAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    ///     Resource attributes for policy evaluation
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    ///     Environmental attributes (time, location, etc.)
    /// </summary>
    public Dictionary<string, object> EnvironmentalAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    ///     Action being requested
    /// </summary>
    public Dictionary<string, object> ActionAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    ///     Requested permission type
    /// </summary>
    public PermissionType RequestedPermission { get; set; }

    /// <summary>
    ///     User ID making the request
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Tenant ID context
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     Resource ID being accessed
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    ///     Resource type name
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    ///     Content type name
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    ///     Session ID for tracking
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    ///     Request timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     Client IP address
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    ///     User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Add user attribute
    /// </summary>
    public void AddUserAttribute(string key, object value) { UserAttributes[key] = value; }

    /// <summary>
    ///     Add resource attribute
    /// </summary>
    public void AddResourceAttribute(string key, object value) { ResourceAttributes[key] = value; }

    /// <summary>
    ///     Add environmental attribute
    /// </summary>
    public void AddEnvironmentalAttribute(string key, object value) { EnvironmentalAttributes[key] = value; }

    /// <summary>
    ///     Add action attribute
    /// </summary>
    public void AddActionAttribute(string key, object value) { ActionAttributes[key] = value; }

    /// <summary>
    ///     Get attribute value safely
    /// </summary>
    public T? GetAttribute<T>(string category, string key)
    {
        var attributes = category.ToLower() switch
        {
            "user" => UserAttributes,
            "resource" => ResourceAttributes,
            "environment" => EnvironmentalAttributes,
            "action" => ActionAttributes,
            _ => new Dictionary<string, object>()
        };

        return attributes.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
    }

    /// <summary>
    ///     Check if attribute exists
    /// </summary>
    public bool HasAttribute(string category, string key)
    {
        var attributes = category.ToLower() switch
        {
            "user" => UserAttributes,
            "resource" => ResourceAttributes,
            "environment" => EnvironmentalAttributes,
            "action" => ActionAttributes,
            _ => new Dictionary<string, object>()
        };

        return attributes.ContainsKey(key);
    }
}
