namespace GameGuild.Identity.Authentication;

/// <summary>
///     Context information for conditional policy evaluation
/// </summary>
public abstract class ConditionalPolicyContext
{
    /// <summary>
    ///     Current timestamp
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
    ///     Location information
    /// </summary>
    public PolicyLocationInfo? Location { get; set; }

    /// <summary>
    ///     Device information
    /// </summary>
    public DeviceInfo? Device { get; set; }

    /// <summary>
    ///     Environment name (Production, Staging, Development)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    ///     Session information
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    ///     Risk score (0-100)
    /// </summary>
    public int? RiskScore { get; set; }

    /// <summary>
    ///     Additional custom attributes
    /// </summary>
    public Dictionary<string, object> CustomAttributes { get; set; } = new Dictionary<string, object>();
}
