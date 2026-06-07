



namespace GameGuild.Monitoring.SLA;

public class SlaMonitoringService(
    IServiceLevelObjectiveRepository sloRepository,
    IServiceLevelIndicatorRepository sliRepository,
    ISloViolationRepository violationRepository,
    IErrorBudgetCalculator errorBudgetCalculator,
    IAlertManager alertManager
) : ISlaMonitoringService
{
    public async Task RecordMetricAsync(SliMetricDto metric, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metric);

        if (metric.ServiceLevelObjectiveId == Guid.Empty)
        {
            throw new ArgumentException("Service level objective id is required.", nameof(metric));
        }

        var slo = await sloRepository
            .GetByIdAsync(metric.ServiceLevelObjectiveId, cancellationToken)
            .ConfigureAwait(false);

        if (slo is null)
        {
            throw new InvalidOperationException($"SLO with ID '{metric.ServiceLevelObjectiveId}' not found.");
        }

        var indicator = metric.IsSuccessful
            ? ServiceLevelIndicator.CreateSuccess(
                metric.ServiceLevelObjectiveId,
                metric.Value,
                metric.ResponseTimeMs,
                metric.StatusCode,
                metric.Endpoint)
            : ServiceLevelIndicator.CreateFailure(
                metric.ServiceLevelObjectiveId,
                metric.Value,
                metric.ErrorMessage ?? "Unknown error",
                metric.ResponseTimeMs,
                metric.StatusCode,
                metric.Endpoint);

        indicator.Timestamp = metric.Timestamp ?? DateTimeOffset.UtcNow;
        indicator.Metadata = metric.Metadata;

        if (slo.TenantId.HasValue)
        {
            indicator.SetTenantId(slo.TenantId.Value);
        }

        await sliRepository.AddAsync(indicator, cancellationToken).ConfigureAwait(false);
        await EvaluateSloAsync(metric.ServiceLevelObjectiveId, cancellationToken).ConfigureAwait(false);
    }

    public async Task EvaluateAllSlosAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var enabledSlos = await sloRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var filteredSlos = enabledSlos.Where(s => s.IsEnabled);

        foreach (var slo in filteredSlos)
        {
            try { await EvaluateSloAsync(slo.Id, cancellationToken).ConfigureAwait(false); }
            catch
            {
                // Log error but continue with other SLOs
            }
        }
    }

    public async Task EvaluateSloAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo is not { IsEnabled: true }) { return; }

        // Calculate error budget
        var errorBudget = await errorBudgetCalculator.CalculateAsync(sloId, cancellationToken).ConfigureAwait(false);

        // Update SLO status
        slo.UpdateStatus(errorBudget.ActualPercentage);
        slo.LastEvaluatedAt = DateTimeOffset.UtcNow;
        slo.CurrentActualPercentage = errorBudget.ActualPercentage;
        slo.RemainingErrorBudget = errorBudget.RemainingBudgetPercentage;

        await sloRepository.UpdateAsync(slo, cancellationToken).ConfigureAwait(false);

        // Check and trigger alerts if necessary
        await alertManager.CheckAndTriggerAlertAsync(slo, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SloComplianceDto> GetComplianceAsync(Guid sloId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdWithViolationsAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID '{sloId}' not found."); }

        var errorBudget = await errorBudgetCalculator.CalculateAsync(sloId, cancellationToken).ConfigureAwait(false);

        // Use provided dates or default to SLO time window
        var startTime = startDate ?? DateTimeOffset.UtcNow.AddDays(-slo.TimeWindowDays);
        var endTime = endDate ?? DateTimeOffset.UtcNow;

        var violations = slo.Violations.Where(v => v.StartedAt >= startTime && v.StartedAt <= endTime).OrderByDescending(v => v.StartedAt).ToList();

        var totalViolationDuration = violations.Where(v => v.EndedAt.HasValue).Sum(v => (v.EndedAt!.Value - v.StartedAt).TotalMinutes);

        return new SloComplianceDto
        {
            ServiceLevelObjectiveId = slo.Id,
            Name = slo.Name,
            ServiceName = slo.ServiceName,
            TargetPercentage = slo.TargetPercentage,
            ActualPercentage = errorBudget.ActualPercentage,
            IsCompliant = errorBudget.ActualPercentage >= slo.TargetPercentage,
            Status = slo.Status,
            TimeWindowDays = slo.TimeWindowDays,
            PeriodStart = startTime,
            PeriodEnd = endTime,
            TotalMeasurements = errorBudget.TotalRequests,
            SuccessfulMeasurements = errorBudget.SuccessfulRequests,
            ViolationCount = violations.Count,
            TotalDowntimeMinutes = totalViolationDuration
        };
    }

    public async Task<ErrorBudgetDto> GetErrorBudgetAsync(Guid sloId, CancellationToken cancellationToken = default) { return await errorBudgetCalculator.CalculateAsync(sloId, cancellationToken).ConfigureAwait(false); }

    public async Task CheckErrorBudgetAlertsAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo is not { IsEnabled: true }) { return; }

        var errorBudget = await errorBudgetCalculator.CalculateAsync(sloId, cancellationToken).ConfigureAwait(false);

        // Check if we should create a violation
        if (errorBudget.RemainingBudgetPercentage <= 0)
        {
            var existingViolation = await violationRepository.GetOngoingViolationsAsync(sloId, cancellationToken).ConfigureAwait(false);

            if (existingViolation.Count == 0)
            {
                var violation = new SloViolation
                {
                    Id = Guid.NewGuid(),
                    ServiceLevelObjectiveId = sloId,
                    StartedAt = DateTimeOffset.UtcNow,
                    ActualValue = errorBudget.ActualPercentage,
                    TargetValue = errorBudget.TargetPercentage,
                    Severity = errorBudget.RemainingBudgetPercentage <= -10 ? ViolationSeverity.Critical : ViolationSeverity.High,
                    Description = $"Error budget exhausted for {slo.Name}. Actual: {errorBudget.ActualPercentage:F2}%, Target: {errorBudget.TargetPercentage:F2}%"
                };
                // TenantId is set automatically via EntityBase

                await violationRepository.AddAsync(violation, cancellationToken).ConfigureAwait(false);
            }
        }

        // Trigger alerts based on thresholds
        await alertManager.CheckAndTriggerAlertAsync(slo, cancellationToken).ConfigureAwait(false);
    }

    public async Task<double> GetErrorBudgetBurnRateAsync(Guid sloId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo == null) { return 0; }

        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate - window;

        var totalRequests = await sliRepository.GetTotalCountAsync(sloId, startDate, endDate, cancellationToken).ConfigureAwait(false);
        var successfulRequests = await sliRepository.GetSuccessfulCountAsync(sloId, startDate, endDate, cancellationToken).ConfigureAwait(false);
        var failedRequests = totalRequests - successfulRequests;

        if (totalRequests == 0) { return 0; }

        var errorRate = failedRequests / (double) totalRequests * 100;
        var errorBudget = 100 - slo.TargetPercentage;

        // Burn rate = (error rate / error budget) normalized to per-day rate
        var burnRate = errorBudget > 0 ? errorRate / errorBudget : 0;
        var daysInWindow = window.TotalDays;

        return daysInWindow > 0 ? burnRate / daysInWindow : burnRate;
    }

    public async Task<IEnumerable<SloViolationDto>> GetActiveSloViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var violations = await violationRepository.GetAllOngoingViolationsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var dtos = new List<SloViolationDto>();

        foreach (var violation in violations)
        {
            var slo = await sloRepository.GetByIdAsync(violation.ServiceLevelObjectiveId, cancellationToken).ConfigureAwait(false);

            if (slo != null)
            {
                dtos.Add(
                    new SloViolationDto
                    {
                        Id = violation.Id,
                        ServiceLevelObjectiveId = violation.ServiceLevelObjectiveId,
                        SloName = slo.Name,
                        ServiceName = slo.ServiceName,
                        StartedAt = violation.StartedAt,
                        EndedAt = violation.EndedAt,
                        ActualValue = violation.ActualValue,
                        TargetValue = violation.TargetValue,
                        Severity = violation.Severity,
                        Description = violation.Description,
                        Notes = violation.Notes,
                        IsAcknowledged = violation.IsAcknowledged,
                        AcknowledgedAt = violation.AcknowledgedAt,
                        AcknowledgedByUserId = violation.AcknowledgedByUserId,
                        AlertTriggered = violation.AlertTriggered
                    }
                );
            }
        }

        return dtos;
    }

    public async Task<SloComplianceReportDto> GenerateComplianceReportAsync(Guid? tenantId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        // Get all SLOs for the tenant (or all SLOs if tenantId is null)
        var slos = tenantId.HasValue ? await sloRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken) : await sloRepository.GetAllSlosAsync(cancellationToken).ConfigureAwait(false);

        var summaries = new List<SloComplianceSummaryDto>();
        var compliantCount = 0;
        var violatedCount = 0;

        foreach (var slo in slos)
        {
            var errorBudget = await errorBudgetCalculator.CalculateAsync(slo.Id, cancellationToken).ConfigureAwait(false);
            var violations = await violationRepository.GetBySloIdAndTimeRangeAsync(slo.Id, startDate, endDate, cancellationToken).ConfigureAwait(false);

            var isCompliant = errorBudget.ActualPercentage >= slo.TargetPercentage;

            if (isCompliant) { compliantCount++; }
            else { violatedCount++; }

            summaries.Add(
                new SloComplianceSummaryDto
                {
                    SloId = slo.Id,
                    SloName = slo.Name,
                    ServiceName = slo.ServiceName,
                    IsCompliant = isCompliant,
                    ActualPercentage = errorBudget.ActualPercentage,
                    TargetPercentage = slo.TargetPercentage,
                    ViolationCount = violations.Count(),
                    ErrorBudgetRemaining = errorBudget.RemainingBudgetPercentage,
                    Status = slo.Status.ToString()
                }
            );
        }

        var totalSlos = slos.Count;
        var overallCompliance = totalSlos > 0 ? compliantCount / (double) totalSlos * 100 : 100;

        return new SloComplianceReportDto
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            StartDate = startDate,
            EndDate = endDate,
            TenantId = tenantId,
            TotalSlos = totalSlos,
            CompliantSlos = compliantCount,
            ViolatedSlos = violatedCount,
            OverallCompliancePercentage = overallCompliance,
            SloSummaries = summaries
        };
    }
}
