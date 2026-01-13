namespace GameGuild.Resources;

/// <summary>
///     Resource limit check response.
///     <para>
///     <b>ADVISORY:</b> This response indicates whether an operation WOULD be allowed,
///     but does not guarantee quota availability. For authoritative enforcement,
///     use <c>TryAtomicConsumeAsync</c> or the <c>[RequiresQuota]</c> attribute.
///     </para>
/// </summary>
public class ResourceLimitCheckResponse
{
    public ResourceUsageType Type { get; set; }

    public long Current { get; set; }

    public long Limit { get; set; }

    public long CurrentUsage { get; set; }

    public long? SoftLimit { get; set; }

    public long? HardLimit { get; set; }

    /// <summary>
    ///     Whether the operation can proceed based on current quota state.
    ///     <para>
    ///     <b>WARNING:</b> This is advisory only. Under concurrent access, quota state
    ///     may change between this check and actual operation execution.
    ///     For guaranteed enforcement, use <c>TryAtomicConsumeAsync</c>.
    ///     </para>
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    ///     Whether the soft limit would be exceeded (warning threshold).
    ///     Soft limit warnings should trigger alerts but not block operations.
    /// </summary>
    public bool IsSoftLimitWarning => SoftLimit.HasValue && CurrentUsage >= SoftLimit.Value;

    /// <summary>
    ///     The percentage of hard limit currently used (0-100+).
    ///     Returns 0 if no hard limit is set.
    /// </summary>
    public double UsagePercentage => HardLimit.HasValue && HardLimit.Value > 0
        ? (double)CurrentUsage / HardLimit.Value * 100
        : 0;

    /// <summary>
    ///     Remaining quota before hard limit is reached.
    ///     Returns null if no hard limit is set (unlimited).
    /// </summary>
    public long? RemainingQuota => HardLimit.HasValue
        ? Math.Max(0, HardLimit.Value - CurrentUsage)
        : null;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Throws <see cref="QuotaExceededException"/> if the operation cannot proceed.
    ///     Use this for mandatory enforcement when you've already checked advisorily.
    /// </summary>
    /// <param name="tenantId">Tenant ID for the exception</param>
    /// <param name="requestedAmount">The amount that was requested</param>
    /// <exception cref="QuotaExceededException">Thrown when CanProceed is false</exception>
    public void ThrowIfExceeded(Guid tenantId, long requestedAmount = 1)
    {
        if (!CanProceed)
        {
            throw new QuotaExceededException(
                $"Resource quota exceeded for {Type}. Current: {CurrentUsage}, Limit: {HardLimit}, Requested: {requestedAmount}",
                Type,
                CurrentUsage,
                HardLimit ?? 0,
                tenantId);
        }
    }
}
