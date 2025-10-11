namespace GameGuild.Modules.Resources;

/// <summary>
///     Throttling policy for resource consumption
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

/// <summary>
///     Resource throttling policy for managing consumption rates
/// </summary>
[Table("resource_throttling_policies")]
[Index(nameof(TenantId), nameof(ResourceType), IsUnique = true)]
public class ResourceThrottlingPolicy : EntityBase
{
    /// <summary>
    ///     Tenant this policy applies to
    /// </summary>
    public override Guid? TenantId { get; set; }

    /// <summary>
    ///     Type of resource being throttled
    /// </summary>
    public ResourceUsageType ResourceType { get; set; }

    /// <summary>
    ///     Throttling strategy to apply
    /// </summary>
    public ThrottlingStrategy Strategy { get; set; } = ThrottlingStrategy.None;

    /// <summary>
    ///     Whether this policy is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Threshold percentage at which throttling begins (e.g., 80 = start at 80% of quota)
    /// </summary>
    public int ThrottlingThresholdPercent { get; set; } = 80;

    /// <summary>
    ///     Maximum requests per time window (for rate limiting)
    /// </summary>
    public int? MaxRequestsPerWindow { get; set; }

    /// <summary>
    ///     Time window duration in seconds (for rate limiting)
    /// </summary>
    public int? WindowDurationSeconds { get; set; }

    /// <summary>
    ///     Degradation factor (0.0-1.0) - how much to slow down requests
    /// </summary>
    public decimal DegradationFactor { get; set; } = 0.5m;

    /// <summary>
    ///     Priority threshold - requests below this priority are throttled first
    /// </summary>
    public int? PriorityThreshold { get; set; }

    /// <summary>
    ///     Additional configuration as JSON
    /// </summary>
    [MaxLength(2000)]
    public string? Configuration { get; set; }

    /// <summary>
    ///     Calculate delay in milliseconds based on current usage percentage
    /// </summary>
    public int CalculateDelayMs(double usagePercentage)
    {
        if (!IsActive || usagePercentage < ThrottlingThresholdPercent)
            return 0;

        return Strategy switch
        {
            ThrottlingStrategy.GradualDegradation => CalculateGradualDelay(usagePercentage),
            ThrottlingStrategy.HardCutoff => usagePercentage >= 100 ? int.MaxValue : 0,
            ThrottlingStrategy.RateLimiting => CalculateRateLimitDelay(),
            _ => 0
        };
    }

    private int CalculateGradualDelay(double usagePercentage)
    {
        // Linear increase in delay as usage approaches 100%
        var excessPercentage = usagePercentage - ThrottlingThresholdPercent;
        var maxExcess = 100 - ThrottlingThresholdPercent;
        var delayFactor = excessPercentage / maxExcess;
        
        // Max delay of 5 seconds, scaled by degradation factor
        return (int)(5000 * delayFactor * (double)DegradationFactor);
    }

    private int CalculateRateLimitDelay()
    {
        if (!MaxRequestsPerWindow.HasValue || !WindowDurationSeconds.HasValue)
            return 0;

        // Calculate minimum delay between requests
        return (WindowDurationSeconds.Value * 1000) / MaxRequestsPerWindow.Value;
    }

    /// <summary>
    ///     Check if request should be throttled based on priority
    /// </summary>
    public bool ShouldThrottleByPriority(int requestPriority)
    {
        if (!IsActive || Strategy != ThrottlingStrategy.PriorityBased)
            return false;

        return PriorityThreshold.HasValue && requestPriority < PriorityThreshold.Value;
    }
}
