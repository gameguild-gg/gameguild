namespace GameGuild.Modules.SlaMonitoring.Services;

/// <summary>
/// Service for managing SLA/SLO monitoring and error budgets.
/// </summary>
public interface ISlaMonitoringService
{
    /// <summary>
    /// Records a service level indicator metric.
    /// </summary>
    Task RecordSliMetricAsync(Guid sloId, double value, bool isSuccessful, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the current error budget for an SLO.
    /// </summary>
    Task<ErrorBudgetDto> CalculateErrorBudgetAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets SLO compliance status for a time window.
    /// </summary>
    Task<SloComplianceDto> GetComplianceStatusAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if error budget alerts should be triggered.
    /// </summary>
    Task CheckErrorBudgetAlertsAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the burn rate for an error budget.
    /// </summary>
    Task<double> GetErrorBudgetBurnRateAsync(Guid sloId, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active SLO violations.
    /// </summary>
    Task<IEnumerable<SloViolationDto>> GetActiveSloViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an SLO compliance report for a time period.
    /// </summary>
    Task<SloComplianceReportDto> GenerateComplianceReportAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the status of a Service Level Objective.
/// </summary>
public enum SloStatus
{
    Active = 0,
    Breached = 1,
    AtRisk = 2,
    Disabled = 3
}

/// <summary>
/// DTO for error budget information.
/// </summary>
public record ErrorBudgetDto(
    Guid SloId,
    string SloName,
    double TargetPercentage,
    double ActualPercentage,
    double RemainingBudgetPercentage,
    double BurnRate,
    DateTime WindowStart,
    DateTime WindowEnd,
    long TotalRequests,
    long FailedRequests,
    TimeSpan EstimatedTimeToExhaustion
);

/// <summary>
/// DTO for SLO compliance information.
/// </summary>
public record SloComplianceDto(
    Guid SloId,
    string SloName,
    bool IsCompliant,
    double ActualPercentage,
    double TargetPercentage,
    int ViolationCount,
    DateTime EvaluationStart,
    DateTime EvaluationEnd
);

/// <summary>
/// DTO for SLO violation information.
/// </summary>
public record SloViolationDto(
    Guid Id,
    Guid SloId,
    string SloName,
    DateTime StartedAt,
    DateTime? EndedAt,
    double ActualValue,
    double TargetValue,
    string Severity,
    string? Notes
);

/// <summary>
/// DTO for SLO compliance report.
/// </summary>
public record SloComplianceReportDto(
    DateTime GeneratedAt,
    DateTime StartDate,
    DateTime EndDate,
    int TotalSlos,
    int CompliantSlos,
    int ViolatedSlos,
    double OverallCompliancePercentage,
    IEnumerable<SloComplianceSummaryDto> SloSummaries
);

/// <summary>
/// DTO for individual SLO compliance summary.
/// </summary>
public record SloComplianceSummaryDto(
    Guid SloId,
    string SloName,
    string ServiceName,
    bool IsCompliant,
    double ActualPercentage,
    double TargetPercentage,
    int ViolationCount,
    double ErrorBudgetRemaining
);
