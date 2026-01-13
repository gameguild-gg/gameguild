namespace GameGuild.Resources;

/// <summary>
///     Result of resource quota enforcement check.
///     <para>
///     <b>ADVISORY:</b> This result indicates the current quota state at the time of query.
///     Under concurrent access, quota state may change between this check and actual operation.
///     For authoritative enforcement, use commands decorated with <c>[RequiresQuota]</c>.
///     </para>
/// </summary>
public class ResourceQuotaEnforcementResult
{
    /// <summary>
    ///     Whether the request is allowed based on quota limits.
    ///     <para>
    ///     <b>ADVISORY:</b> This is a point-in-time check. For guaranteed enforcement,
    ///     use <c>[RequiresQuota]</c> attribute which uses atomic operations.
    ///     </para>
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    ///     Whether soft limit has been exceeded (warning threshold)
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
    public string? Message { get; set; }

    /// <summary>
    ///     Resource type this enforcement applies to
    /// </summary>
    public ResourceUsageType Type { get; set; }

    /// <summary>
    ///     When the quota will reset next
    /// </summary>
    public DateTime? NextReset { get; set; }

    /// <summary>
    ///     Remaining quota before hard limit is reached.
    ///     Returns null if no hard limit is set (unlimited).
    /// </summary>
    public long? RemainingQuota => HardLimit.HasValue
        ? Math.Max(0, HardLimit.Value - CurrentUsage)
        : null;

    /// <summary>
    ///     Throws <see cref="QuotaExceededException"/> if the operation is not allowed.
    ///     Use this for mandatory enforcement after an advisory check.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the exception</param>
    /// <param name="requestedAmount">The amount that was requested</param>
    /// <exception cref="QuotaExceededException">Thrown when IsAllowed is false</exception>
    public void ThrowIfNotAllowed(Guid tenantId, long requestedAmount = 1)
    {
        if (!IsAllowed)
        {
            throw new QuotaExceededException(
                Message ?? $"Resource quota exceeded for {Type}",
                Type,
                CurrentUsage,
                HardLimit ?? 0,
                tenantId);
        }
    }
}
