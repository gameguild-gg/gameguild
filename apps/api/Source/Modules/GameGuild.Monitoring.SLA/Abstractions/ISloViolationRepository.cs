

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Repository interface for SLO Violations
/// </summary>
public interface ISloViolationRepository
{
    /// <summary>
    ///     Gets a violation by ID
    /// </summary>
    Task<SloViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all violations for an SLO
    /// </summary>
    Task<List<SloViolation>> GetBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets violations for an SLO within a time range
    /// </summary>
    Task<List<SloViolation>> GetBySloIdAndTimeRangeAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all violations for a tenant
    /// </summary>
    Task<List<SloViolation>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets ongoing violations for an SLO
    /// </summary>
    Task<List<SloViolation>> GetOngoingViolationsAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all ongoing violations optionally filtered by tenant
    /// </summary>
    Task<List<SloViolation>> GetAllOngoingViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets violations by severity
    /// </summary>
    Task<List<SloViolation>> GetBySeverityAsync(ViolationSeverity severity, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets unacknowledged violations
    /// </summary>
    Task<List<SloViolation>> GetUnacknowledgedAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets violations that triggered alerts
    /// </summary>
    Task<List<SloViolation>> GetWithAlertsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new violation
    /// </summary>
    Task<SloViolation> AddAsync(SloViolation violation, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing violation
    /// </summary>
    Task UpdateAsync(SloViolation violation, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts violations for an SLO in a time range
    /// </summary>
    Task<int> CountViolationsAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);
}
