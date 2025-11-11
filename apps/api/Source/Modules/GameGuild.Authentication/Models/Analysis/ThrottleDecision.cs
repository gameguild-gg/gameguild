namespace GameGuild.Authentication.Models.Analysis;

/// <summary>
///     Decision on whether to throttle a request to prevent enumeration attacks.
/// </summary>
public class ThrottleDecision
{
    /// <summary>
    ///     Whether the request should be throttled.
    /// </summary>
    public bool ShouldThrottle { get; set; }

    /// <summary>
    ///     How long to delay the response (in milliseconds).
    /// </summary>
    public int DelayMs { get; set; }

    /// <summary>
    ///     Reason for throttling.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Number of recent attempts from this identifier.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    ///     Time window analyzed (in minutes).
    /// </summary>
    public int TimeWindowMinutes { get; set; }

    /// <summary>
    ///     When the throttle will be lifted.
    /// </summary>
    public DateTime? ThrottleUntil { get; set; }

    /// <summary>
    ///     Additional throttle metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
