
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Result of bulk subscription renewals
/// </summary>
public abstract class BulkRenewalResult
{
    /// <summary>
    ///     Total number of subscriptions processed
    /// </summary>
    public int TotalProcessed { get; init; }

    /// <summary>
    ///     Number of successful renewals
    /// </summary>
    public int SuccessfulRenewals { get; init; }

    /// <summary>
    ///     Number of failed renewals
    /// </summary>
    public int FailedRenewals { get; init; }

    /// <summary>
    ///     Details of each renewal attempt
    /// </summary>
    public List<RenewalAttempt> RenewalAttempts { get; init; } = new List<RenewalAttempt>();

    /// <summary>
    ///     Total revenue from successful renewals
    /// </summary>
    public Money TotalRevenue { get; init; } = Money.Zero();

    /// <summary>
    ///     Processing start time
    /// </summary>
    public DateTime ProcessedAt { get; init; } = SystemClock.UtcNow;

    /// <summary>
    ///     Success rate percentage
    /// </summary>
    public decimal SuccessRate { get => TotalProcessed > 0 ? (decimal) SuccessfulRenewals / TotalProcessed * 100 : 0; }
}
