
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Core service interface for SLA monitoring operations
/// </summary>
public interface ISlaMonitoringService
{
    /// <summary>
    ///     Records a service level indicator metric
    /// </summary>
    /// <param name="metric">SLI metric data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordMetricAsync(SliMetricDto metric, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates all enabled SLOs for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EvaluateAllSlosAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates a specific SLO
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EvaluateSloAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets compliance information for an SLO
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="startDate">Start date for compliance calculation (optional, defaults to SLO evaluation window)</param>
    /// <param name="endDate">End date for compliance calculation (optional, defaults to now)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compliance information</returns>
    Task<SloComplianceDto> GetComplianceAsync(Guid sloId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets error budget information for an SLO
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Error budget information</returns>
    Task<ErrorBudgetDto> GetErrorBudgetAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if error budget alerts should be triggered for an SLO
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CheckErrorBudgetAlertsAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the error budget burn rate for an SLO over a specified time window
    /// </summary>
    /// <param name="sloId">SLO identifier</param>
    /// <param name="window">Time window for burn rate calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Burn rate as a percentage per day</returns>
    Task<double> GetErrorBudgetBurnRateAsync(Guid sloId, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active SLO violations for a tenant
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier (null for all tenants)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active violation DTOs</returns>
    Task<IEnumerable<SloViolationDto>> GetActiveSloViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Generates a comprehensive SLO compliance report for a time period
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier (null for all tenants)</param>
    /// <param name="startDate">Start date of reporting period</param>
    /// <param name="endDate">End date of reporting period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive compliance report</returns>
    Task<SloComplianceReportDto> GenerateComplianceReportAsync(Guid? tenantId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
}
