namespace GameGuild.Subscriptions.Models;

/// <summary>
///     Result of individual limit check
/// </summary>
public abstract class LimitCheckResult
{
    /// <summary>
    ///     Name of the limit being checked
    /// </summary>
    public string LimitName { get; init; } = string.Empty;

    /// <summary>
    ///     Current usage
    /// </summary>
    public long CurrentUsage { get; init; }

    /// <summary>
    ///     Maximum allowed by plan
    /// </summary>
    public long MaxAllowed { get; init; }

    /// <summary>
    ///     Whether this check passed
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    ///     Usage percentage (CurrentUsage / MaxAllowed * 100)
    /// </summary>
    public decimal UsagePercentage { get => MaxAllowed > 0 ? (decimal) CurrentUsage / MaxAllowed * 100 : 0; }

    /// <summary>
    ///     How much the usage exceeds the limit (0 if within limits)
    /// </summary>
    public long ExcessUsage { get => Math.Max(0, CurrentUsage - MaxAllowed); }
}
