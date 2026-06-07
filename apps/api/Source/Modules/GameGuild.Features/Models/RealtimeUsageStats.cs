namespace GameGuild.Features;

/// <summary>
///     Real-time usage statistics
/// </summary>
public sealed class RealtimeUsageStats
{
    /// <summary>
    ///     Current timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     Total evaluations in the last minute
    /// </summary>
    public long EvaluationsLastMinute { get; set; }

    /// <summary>
    ///     Total evaluations in the last hour
    /// </summary>
    public long EvaluationsLastHour { get; set; }

    /// <summary>
    ///     Total evaluations today
    /// </summary>
    public long EvaluationsToday { get; set; }

    /// <summary>
    ///     Active feature count
    /// </summary>
    public int ActiveFeatureCount { get; set; }

    /// <summary>
    ///     Current error rate (percentage)
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    ///     Average evaluation latency in milliseconds
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    ///     Cache hit rate (percentage)
    /// </summary>
    public double CacheHitRate { get; set; }
}
