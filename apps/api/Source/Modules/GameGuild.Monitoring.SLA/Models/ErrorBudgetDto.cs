namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Data transfer object for error budget information
/// </summary>
public class ErrorBudgetDto
{
    /// <summary>
    ///     SLO identifier
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    ///     Target percentage (e.g., 99.9)
    /// </summary>
    public double TargetPercentage { get; set; }

    /// <summary>
    ///     Error budget percentage (100 - Target)
    /// </summary>
    public double ErrorBudgetPercentage { get; set; }

    /// <summary>
    ///     Current actual performance percentage
    /// </summary>
    public double ActualPercentage { get; set; }

    /// <summary>
    ///     Total number of requests in the time window
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    ///     Number of successful requests
    /// </summary>
    public long SuccessfulRequests { get; set; }

    /// <summary>
    ///     Number of failed requests
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    ///     Number of allowed failures based on error budget
    /// </summary>
    public long AllowedFailures { get; set; }

    /// <summary>
    ///     Remaining error budget count
    /// </summary>
    public long RemainingBudget { get; set; }

    /// <summary>
    ///     Remaining error budget as a percentage
    /// </summary>
    public double RemainingBudgetPercentage { get; set; }

    /// <summary>
    ///     Rate at which the error budget is being consumed (failures per day)
    /// </summary>
    public double BurnRate { get; set; }

    /// <summary>
    ///     Estimated time until error budget is exhausted (in hours)
    /// </summary>
    public double? TimeToExhaustionHours { get; set; }

    /// <summary>
    ///     Time window in days
    /// </summary>
    public int TimeWindowDays { get; set; }

    /// <summary>
    ///     Start of the current time window
    /// </summary>
    public DateTimeOffset WindowStart { get; set; }

    /// <summary>
    ///     End of the current time window
    /// </summary>
    public DateTimeOffset WindowEnd { get; set; }

    /// <summary>
    ///     Whether the error budget is healthy
    /// </summary>
    public bool IsHealthy { get; set; }
}
