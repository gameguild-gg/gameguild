namespace GameGuild.Notifications.Services.Email;

/// <summary>
/// Email dispatcher tuning. Bound to configuration section "Notifications:EmailDispatcher".
/// </summary>
public sealed class EmailDispatcherOptions
{
    /// <summary>Delay between sweep passes.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Total delivery attempts before a row is deadlettered.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Retry backoff per failed attempt; index is the pre-increment AttemptCount, last entry repeats.</summary>
    public TimeSpan[] BackoffSchedule { get; set; } =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2), TimeSpan.FromHours(8)];

    /// <summary>Transactional emails older than this are deadlettered as stale instead of sent.</summary>
    public TimeSpan TransactionalStalenessTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Maximum rows processed per sweep.</summary>
    public int SweepBatchSize { get; set; } = 50;
}
