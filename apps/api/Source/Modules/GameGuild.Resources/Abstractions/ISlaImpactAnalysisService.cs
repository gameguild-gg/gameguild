using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;

namespace GameGuild.Resources.Abstractions;

/// <summary>
///     Service for SLA impact analysis and violation tracking
/// </summary>
public interface ISlaImpactAnalysisService
{
    /// <summary>
    ///     Record an SLA violation
    /// </summary>
    Task<SlaImpactAnalysis> RecordViolationAsync(
        Guid resourceQuotaId,
        SlaViolationType violationType,
        SlaViolationSeverity severity,
        long expectedValue,
        long actualValue,
        Guid? userId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Get violation by ID
    /// </summary>
    Task<SlaImpactAnalysis?> GetViolationAsync(Guid violationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all violations for a tenant
    /// </summary>
    Task<IEnumerable<SlaImpactAnalysis>> GetTenantViolationsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SlaViolationSeverity? minSeverity = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Get unresolved violations
    /// </summary>
    Task<IEnumerable<SlaImpactAnalysis>> GetUnresolvedViolationsAsync(Guid? tenantId = null, SlaViolationSeverity? minSeverity = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resolve a violation
    /// </summary>
    Task<bool> ResolveViolationAsync(Guid violationId, Guid resolvedByUserId, string? mitigationActions = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update violation details
    /// </summary>
    Task<bool> UpdateViolationAsync(Guid violationId, string? rootCause = null, string? businessImpact = null, bool? requiresEscalation = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create incident ticket for a violation
    /// </summary>
    Task<string> CreateIncidentTicketAsync(Guid violationId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get SLA compliance metrics for a tenant
    /// </summary>
    Task<SlaComplianceMetrics> GetComplianceMetricsAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get violations by resource type
    /// </summary>
    Task<Dictionary<ResourceUsageType, int>> GetViolationsByResourceTypeAsync(Guid? tenantId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get critical ongoing violations
    /// </summary>
    Task<IEnumerable<SlaImpactAnalysis>> GetCriticalOngoingViolationsAsync(CancellationToken cancellationToken = default);

    // TODO: Integration with Incident Management module for ticket creation
    // TODO: Integration with Monitoring module for real-time alerting
    // TODO: Integration with Notification module for stakeholder alerts
}
