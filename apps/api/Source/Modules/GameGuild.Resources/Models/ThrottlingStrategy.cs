namespace GameGuild.Resources;

/// <summary>
///     Throttling strategy types
/// </summary>
public enum ThrottlingStrategy
{
    /// <summary>
    ///     No throttling applied
    /// </summary>
    None = 0,

    /// <summary>
    ///     Hard cutoff - block all requests when limit reached
    /// </summary>
    HardCutoff = 1,

    /// <summary>
    ///     Gradual degradation - slow down requests as limit approached
    /// </summary>
    GradualDegradation = 2,

    /// <summary>
    ///     Rate limiting - limit requests per time window
    /// </summary>
    RateLimiting = 3,

    /// <summary>
    ///     Priority-based - throttle low-priority requests first
    /// </summary>
    PriorityBased = 4
}
