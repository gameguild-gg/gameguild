
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Data transfer object for SLO compliance information
/// </summary>
public class SloComplianceDto
{
    /// <summary>
    ///     SLO identifier
    /// </summary>
    public Guid ServiceLevelObjectiveId { get; set; }

    /// <summary>
    ///     SLO name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Service name
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    ///     Target percentage
    /// </summary>
    public double TargetPercentage { get; set; }

    /// <summary>
    ///     Actual percentage achieved
    /// </summary>
    public double ActualPercentage { get; set; }

    /// <summary>
    ///     Whether the SLO is in compliance
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    ///     Current status
    /// </summary>
    public SloStatus Status { get; set; }

    /// <summary>
    ///     Time window for compliance calculation
    /// </summary>
    public int TimeWindowDays { get; set; }

    /// <summary>
    ///     Start of the compliance period
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <summary>
    ///     End of the compliance period
    /// </summary>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>
    ///     Total measurements in the period
    /// </summary>
    public long TotalMeasurements { get; set; }

    /// <summary>
    ///     Successful measurements
    /// </summary>
    public long SuccessfulMeasurements { get; set; }

    /// <summary>
    ///     Number of violations in the period
    /// </summary>
    public int ViolationCount { get; set; }

    /// <summary>
    ///     Total downtime in the period (minutes)
    /// </summary>
    public double TotalDowntimeMinutes { get; set; }

    /// <summary>
    ///     Remaining error budget percentage
    /// </summary>
    public double? RemainingErrorBudget { get; set; }

    /// <summary>
    ///     When this was last calculated
    /// </summary>
    public DateTimeOffset CalculatedAt { get; set; }
}
