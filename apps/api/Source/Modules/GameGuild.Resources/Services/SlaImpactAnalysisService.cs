using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

/// <summary>
///     Implementation of SLA impact analysis and violation tracking
/// </summary>
public class SlaImpactAnalysisService(
    ISlaImpactAnalysisRepository analysisRepository,
    IResourceQuotaRepository quotaRepository,
    IPublisher publisher,
    ILogger<SlaImpactAnalysisService> logger) : ISlaImpactAnalysisService
{
    public async Task<SlaImpactAnalysis> RecordViolationAsync(
        Guid resourceQuotaId,
        SlaViolationType violationType,
        SlaViolationSeverity severity,
        long expectedValue,
        long actualValue,
        Guid? userId = null,
        CancellationToken cancellationToken = default
    )
    {
        var quota = await quotaRepository.GetByIdAsync(resourceQuotaId, cancellationToken);

        if (quota == null) { throw new ArgumentException($"Resource quota {resourceQuotaId} not found", nameof(resourceQuotaId)); }

        var violation = new SlaImpactAnalysis
        {
            ResourceQuotaId = resourceQuotaId,
            UserId = userId,
            ViolationStartTime = DateTime.UtcNow,
            ViolationType = violationType,
            Severity = severity,
            ExpectedValue = expectedValue,
            ActualValue = actualValue,
            IsResolved = false,
            RequiresEscalation = severity >= SlaViolationSeverity.High,
            IncidentCreated = false
        };

        // Set TenantId using SetProperties (EntityBase has protected setter)
        violation.SetProperties(new Dictionary<string, object?> { ["TenantId"] = quota.TenantId });

        violation.CalculateDeviation();

        var savedViolation = await analysisRepository.CreateAsync(violation, cancellationToken);

        logger.LogWarning("SLA violation recorded: Type={Type}, Severity={Severity}, Quota={QuotaId}, Expected={Expected}, Actual={Actual}", violationType, severity, resourceQuotaId, expectedValue, actualValue);

        // Publish domain event for notification/incident creation
        await publisher.Publish(new SlaViolationRecordedEvent(
            ViolationId: savedViolation.Id,
            TenantId: quota.TenantId!.Value,
            ResourceQuotaId: resourceQuotaId,
            ViolationType: violationType,
            Severity: severity,
            ExpectedValue: expectedValue,
            ActualValue: actualValue,
            DeviationPercentage: savedViolation.DeviationPercentage,
            RequiresEscalation: savedViolation.RequiresEscalation,
            UserId: userId,
            Timestamp: DateTime.UtcNow), cancellationToken);

        // Auto-create incident ticket if escalation required
        if (savedViolation.RequiresEscalation && !savedViolation.IncidentCreated)
        {
            try
            {
                var ticketId = await CreateIncidentTicketAsync(savedViolation.Id, cancellationToken);
                logger.LogInformation("Auto-created incident ticket {TicketId} for SLA violation {ViolationId}", ticketId, savedViolation.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-create incident ticket for SLA violation {ViolationId}", savedViolation.Id);
            }
        }

        return savedViolation;
    }

    public async Task<SlaImpactAnalysis?> GetViolationAsync(Guid violationId, CancellationToken cancellationToken = default) { return await analysisRepository.GetByIdAsync(violationId, cancellationToken); }

    public async Task<IEnumerable<SlaImpactAnalysis>> GetTenantViolationsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        SlaViolationSeverity? minSeverity = null,
        CancellationToken cancellationToken = default
    )
    {
        IEnumerable<SlaImpactAnalysis> violations;

        if (fromDate.HasValue && toDate.HasValue) { violations = await analysisRepository.GetByDateRangeAsync(tenantId, fromDate.Value, toDate.Value, cancellationToken); }
        else
        {
            violations = await analysisRepository.GetByTenantAsync(tenantId, cancellationToken);

            if (fromDate.HasValue) violations = violations.Where(v => v.ViolationStartTime >= fromDate.Value);
            if (toDate.HasValue) violations = violations.Where(v => v.ViolationStartTime <= toDate.Value);
        }

        if (minSeverity.HasValue) { violations = violations.Where(v => v.Severity >= minSeverity.Value); }

        return violations;
    }

    public async Task<IEnumerable<SlaImpactAnalysis>> GetUnresolvedViolationsAsync(Guid? tenantId = null, SlaViolationSeverity? minSeverity = null, CancellationToken cancellationToken = default)
    {
        if (!tenantId.HasValue)
        {
            // If no tenant specified, we can't use the repository method
            // This would need a different repository method or throw an exception
            throw new ArgumentException("TenantId is required", nameof(tenantId));
        }

        var violations = await analysisRepository.GetUnresolvedAsync(tenantId.Value, cancellationToken);

        if (minSeverity.HasValue) { violations = violations.Where(v => v.Severity >= minSeverity.Value); }

        return violations;
    }

    public async Task<bool> ResolveViolationAsync(Guid violationId, Guid resolvedByUserId, string? mitigationActions = null, CancellationToken cancellationToken = default)
    {
        var violation = await analysisRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null) return false;

        violation.Resolve(resolvedByUserId, mitigationActions);

        await analysisRepository.UpdateAsync(violation, cancellationToken);

        logger.LogInformation("SLA violation {ViolationId} resolved by user {UserId}", violationId, resolvedByUserId);

        // Publish domain event for resolution tracking
        await publisher.Publish(new SlaViolationResolvedEvent(
            ViolationId: violationId,
            TenantId: violation.TenantId!.Value,
            ResolvedByUserId: resolvedByUserId,
            ResolutionDuration: violation.ResolvedAt!.Value - violation.ViolationStartTime,
            MitigationActions: mitigationActions,
            Timestamp: DateTime.UtcNow), cancellationToken);

        return true;
    }

    public async Task<bool> UpdateViolationAsync(Guid violationId, string? rootCause = null, string? businessImpact = null, bool? requiresEscalation = null, CancellationToken cancellationToken = default)
    {
        var violation = await analysisRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null) return false;

        if (rootCause != null) violation.RootCause = rootCause;

        if (businessImpact != null) violation.BusinessImpact = businessImpact;

        if (requiresEscalation.HasValue) violation.RequiresEscalation = requiresEscalation.Value;

        violation.UpdatedAt = DateTime.UtcNow;

        await analysisRepository.UpdateAsync(violation, cancellationToken);

        logger.LogInformation("SLA violation {ViolationId} updated", violationId);

        return true;
    }

    public async Task<string> CreateIncidentTicketAsync(Guid violationId, CancellationToken cancellationToken = default)
    {
        var violation = await analysisRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null) { throw new ArgumentException($"Violation {violationId} not found", nameof(violationId)); }

        // TODO: Integration with Incident Management module
        // For now, generate a placeholder ticket ID
        var ticketId = $"INC-{DateTime.UtcNow:yyyyMMdd}-{violationId.ToString().Substring(0, 8).ToUpper()}";

        violation.IncidentCreated = true;
        violation.IncidentTicketId = ticketId;
        violation.UpdatedAt = DateTime.UtcNow;

        await analysisRepository.UpdateAsync(violation, cancellationToken);

        logger.LogInformation("Incident ticket {TicketId} created for SLA violation {ViolationId}", ticketId, violationId);

        return ticketId;
    }

    public async Task<SlaComplianceMetrics> GetComplianceMetricsAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var violations = await analysisRepository.GetByDateRangeAsync(tenantId, periodStart, periodEnd, cancellationToken);

        var violationsList = violations.ToList();

        var totalViolations = violationsList.Count;
        var criticalViolations = violationsList.Count(v => v.Severity == SlaViolationSeverity.Critical);
        var resolvedViolations = violationsList.Count(v => v.IsResolved);
        var unresolvedViolations = totalViolations - resolvedViolations;

        var violationsByType = violationsList.GroupBy(v => v.ViolationType).ToDictionary(g => g.Key, g => g.Count());

        // Calculate average resolution time
        var resolvedWithTime = violationsList.Where(v => v.IsResolved && v.ResolvedAt.HasValue).ToList();

        var avgResolutionTime = resolvedWithTime.Count > 0 ? TimeSpan.FromTicks((long) resolvedWithTime.Select(v => (v.ResolvedAt!.Value - v.ViolationStartTime).Ticks).Average()) : TimeSpan.Zero;

        // Calculate compliance percentage (percentage of time without violations)
        var totalPeriodHours = (periodEnd - periodStart).TotalHours;

        var violationHours = violationsList.Sum(v =>
            {
                var endTime = v.ViolationEndTime ?? DateTime.UtcNow;

                return (endTime - v.ViolationStartTime).TotalHours;
            }
        );

        var compliancePercentage = totalPeriodHours > 0 ? (decimal) ((totalPeriodHours - violationHours) / totalPeriodHours * 100) : 100m;

        var metrics = new SlaComplianceMetrics
        {
            TenantId = tenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalViolations = totalViolations,
            CriticalViolations = criticalViolations,
            ResolvedViolations = resolvedViolations,
            UnresolvedViolations = unresolvedViolations,
            CompliancePercentage = Math.Max(0, Math.Min(100, compliancePercentage)),
            AverageResolutionTime = avgResolutionTime,
            ViolationsByType = violationsByType
        };

        return metrics;
    }

    public async Task<Dictionary<ResourceUsageType, int>> GetViolationsByResourceTypeAsync(Guid? tenantId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        if (!tenantId.HasValue) { throw new ArgumentException("TenantId is required", nameof(tenantId)); }

        var from = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var to = toDate ?? DateTime.UtcNow;

        var stringCounts = await analysisRepository.GetViolationCountsByTypeAsync(tenantId.Value, from, to, cancellationToken);

        // Convert Dictionary<string, int> to Dictionary<ResourceUsageType, int>
        var result = new Dictionary<ResourceUsageType, int>();

        foreach (var kvp in stringCounts)
        {
            if (Enum.TryParse(kvp.Key, out ResourceUsageType usageType)) { result[usageType] = kvp.Value; }
        }

        return result;
    }

    public Task<IEnumerable<SlaImpactAnalysis>> GetCriticalOngoingViolationsAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Repository method requires tenantId but interface doesn't accept one
        // This needs to be resolved - either add tenantId parameter or create a new repository method
        // For now, throw NotImplementedException
        throw new NotImplementedException("GetCriticalOngoingViolationsAsync requires design clarification for tenant filtering");
    }

    // TODO: Integration with Incident Management module for ticket creation
    // TODO: Integration with Monitoring module for real-time alerting
    // TODO: Integration with Notification module for stakeholder alerts
}
