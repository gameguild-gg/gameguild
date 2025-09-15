namespace GameGuild.Modules.Resources.Models;

/// <summary>
/// Response for resource limit checks
/// </summary>
public class ResourceLimitCheckResponse
{
    /// <summary>
    /// Type of resource being checked
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    /// Current usage amount
    /// </summary>
    public long Current { get; set; }

    /// <summary>
    /// Soft limit (warning threshold)
    /// </summary>
    public long? SoftLimit { get; set; }

    /// <summary>
    /// Hard limit (enforcement threshold)
    /// </summary>
    public long? HardLimit { get; set; }

    /// <summary>
    /// Whether the action can proceed
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Whether soft limit has been exceeded
    /// </summary>
    public bool SoftLimitExceeded { get; set; }

    /// <summary>
    /// Whether hard limit has been exceeded
    /// </summary>
    public bool HardLimitExceeded { get; set; }

    /// <summary>
    /// Usage percentage (0-100)
    /// </summary>
    public double UsagePercentage { get; set; }

    /// <summary>
    /// Remaining quota amount
    /// </summary>
    public long? RemainingQuota { get; set; }

    /// <summary>
    /// Message explaining the limit check result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the quota will reset (if applicable)
    /// </summary>
    public DateTime? NextReset { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Create a successful limit check response
    /// </summary>
    public static ResourceLimitCheckResponse Success(
        ResourceUsageType type,
        long current,
        long? softLimit,
        long? hardLimit,
        string? message = null)
    {
        var response = new ResourceLimitCheckResponse
        {
            Type = type,
            Current = current,
            SoftLimit = softLimit,
            HardLimit = hardLimit,
            CanProceed = true,
            SoftLimitExceeded = softLimit.HasValue && current >= softLimit.Value,
            HardLimitExceeded = hardLimit.HasValue && current >= hardLimit.Value,
            Message = message ?? "Usage within limits"
        };

        if (hardLimit.HasValue && hardLimit.Value > 0)
        {
            response.UsagePercentage = (double)current / hardLimit.Value * 100;
            response.RemainingQuota = Math.Max(0, hardLimit.Value - current);
        }

        return response;
    }

    /// <summary>
    /// Create a limit exceeded response
    /// </summary>
    public static ResourceLimitCheckResponse LimitExceeded(
        ResourceUsageType type,
        long current,
        long? softLimit,
        long? hardLimit,
        string message)
    {
        return new ResourceLimitCheckResponse
        {
            Type = type,
            Current = current,
            SoftLimit = softLimit,
            HardLimit = hardLimit,
            CanProceed = false,
            SoftLimitExceeded = softLimit.HasValue && current >= softLimit.Value,
            HardLimitExceeded = hardLimit.HasValue && current >= hardLimit.Value,
            UsagePercentage = hardLimit.HasValue && hardLimit.Value > 0 ? (double)current / hardLimit.Value * 100 : 100,
            RemainingQuota = hardLimit.HasValue ? Math.Max(0, hardLimit.Value - current) : 0,
            Message = message
        };
    }
}
