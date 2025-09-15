namespace GameGuild.Modules.Features.Models;

/// <summary>
/// Context for feature flag evaluation
/// </summary>
public class FeatureContext
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Environment (development, staging, production)
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    /// User roles
    /// </summary>
    public List<string> UserRoles { get; set; } = new();

    /// <summary>
    /// Additional custom attributes
    /// </summary>
    public Dictionary<string, object> CustomAttributes { get; set; } = new();

    /// <summary>
    /// Client IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Result of feature flag evaluation
/// </summary>
public class FeatureEvaluationResult
{
    /// <summary>
    /// Feature key that was evaluated
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether the feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Value returned by the feature flag
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Reason for the evaluation result
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Evaluation timestamp
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Get typed value
    /// </summary>
    public T? GetValue<T>()
    {
        if (Value == null) return default;

        try
        {
            if (Value is T directValue) return directValue;

            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
