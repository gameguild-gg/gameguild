namespace GameGuild.Resources.Models;

/// <summary>
///     Result of resource quota enforcement check
/// </summary>
public class ResourceQuotaEnforcementResult
{
    /// <summary>
    ///     Whether the request is allowed based on quota limits
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    ///     Whether soft limit has been exceeded
    /// </summary>
    public bool IsSoftLimitExceeded { get; set; }

    /// <summary>
    ///     Whether hard limit has been exceeded
    /// </summary>
    public bool IsHardLimitExceeded { get; set; }

    /// <summary>
    ///     Current usage amount
    /// </summary>
    public long CurrentUsage { get; set; }

    /// <summary>
    ///     Soft limit amount (if set)
    /// </summary>
    public long? SoftLimit { get; set; }

    /// <summary>
    ///     Hard limit amount (if set)
    /// </summary>
    public long? HardLimit { get; set; }

    /// <summary>
    ///     Percentage of quota used (0-100)
    /// </summary>
    public double UsagePercentage { get; set; }

    /// <summary>
    ///     Amount that would be exceeded if the request is processed
    /// </summary>
    public long ExcessAmount { get; set; }

    /// <summary>
    ///     Message explaining the enforcement result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Resource type this enforcement applies to
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    ///     When the quota will reset next
    /// </summary>
    public DateTime? NextReset { get; set; }
}
