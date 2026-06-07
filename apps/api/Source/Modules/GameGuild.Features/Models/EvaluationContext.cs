using System.Collections.ObjectModel;

namespace GameGuild.Features;

/// <summary>
///     Evaluation context for feature flag evaluation
/// </summary>
public class EvaluationContext
{
    /// <summary>
    ///     The user identifier for feature targeting
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    ///     The tenant identifier for multi-tenant feature flags
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     The environment where the feature is being evaluated
    /// </summary>
    public string Environment { get; set; } = "production";

    /// <summary>
    ///     Additional custom attributes for feature targeting
    /// </summary>
    public Dictionary<string, object> Attributes { get; } = [];

    /// <summary>
    ///     The session identifier for consistent evaluation
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    ///     The request timestamp for time-based features
    /// </summary>
    public DateTime Timestamp { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     User groups for group-based targeting
    /// </summary>
    public Collection<string> UserGroups { get; } = [];

    /// <summary>
    ///     Geographic location for location-based features
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    ///     Device information for device-based targeting
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    ///     Application version for version-based features
    /// </summary>
    public string? AppVersion { get; set; }
}
