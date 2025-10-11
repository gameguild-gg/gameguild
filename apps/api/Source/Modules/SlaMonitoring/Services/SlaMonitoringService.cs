using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Modules.SlaMonitoring.Repositories;


namespace GameGuild.Modules.SlaMonitoring.Services;

/// <summary>
/// Service implementation for SLA/SLO monitoring.
/// </summary>
public class SlaMonitoringService : ISlaMonitoringService {
    private readonly IServiceLevelObjectiveRepository _sloRepository;
    private readonly IServiceLevelIndicatorRepository _sliRepository;
    private readonly ISloViolationRepository _violationRepository;
    private readonly IErrorBudgetCalculator _errorBudgetCalculator;
    private readonly IAlertManager _alertManager;
    private readonly ILogger<SlaMonitoringService> _logger;

    public SlaMonitoringService(
        IServiceLevelObjectiveRepository sloRepository,
        IServiceLevelIndicatorRepository sliRepository,
        ISloViolationRepository violationRepository,
        IErrorBudgetCalculator errorBudgetCalculator,
        IAlertManager alertManager,
        ILogger<SlaMonitoringService> logger) {
        _sloRepository = sloRepository;
        _sliRepository = sliRepository;
        _violationRepository = violationRepository;
        _errorBudgetCalculator = errorBudgetCalculator;
        _alertManager = alertManager;
        _logger = logger;
    }

    public async Task RecordSliMetricAsync(Guid sloId, double value, bool isSuccessful, CancellationToken cancellationToken = default) {
        var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken);
        if (slo == null) {
            _logger.LogWarning("SLO {SloId} not found", sloId);
            return;
        }

        var sli = new ServiceLevelIndicator {
            Id = Guid.NewGuid(),
            SloId = sloId,
            MetricValue = value,
            IsSuccessful = isSuccessful,
            Timestamp = DateTime.UtcNow
        };

        await _sliRepository.AddAsync(sli, cancellationToken);

        // Check if this metric causes a violation
        var errorBudget = await _errorBudgetCalculator.CalculateAsync(sloId, cancellationToken);
        if (errorBudget.RemainingBudgetPercentage <= 0) {
            await CheckErrorBudgetAlertsAsync(sloId, cancellationToken);
        }

        _logger.LogInformation("Recorded SLI metric for SLO {SloId}: Value={Value}, Successful={IsSuccessful}",
            sloId, value, isSuccessful);
    }

    public async Task<ErrorBudgetDto> CalculateErrorBudgetAsync(Guid sloId, CancellationToken cancellationToken = default) {
        return await _errorBudgetCalculator.CalculateAsync(sloId, cancellationToken);
    }

    public async Task<SloComplianceDto> GetComplianceStatusAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) {
        var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken);
        if (slo == null)
            throw new InvalidOperationException($"SLO {sloId} not found");

        var slis = await _sliRepository.GetBySloIdAsync(sloId, startDate, endDate, cancellationToken);
        var violations = await _violationRepository.GetBySloIdAsync(sloId, startDate, endDate, cancellationToken);

        var totalRequests = slis.Count();
        var successfulRequests = slis.Count(s => s.IsSuccessful);
        var actualPercentage = totalRequests > 0 ? (successfulRequests / (double)totalRequests) * 100 : 100;
        var isCompliant = actualPercentage >= slo.TargetPercentage;

        return new SloComplianceDto(
            sloId,
            slo.Name,
            isCompliant,
            actualPercentage,
            slo.TargetPercentage,
            violations.Count(),
            startDate,
            endDate
        );
    }

    public async Task CheckErrorBudgetAlertsAsync(Guid sloId, CancellationToken cancellationToken = default) {
        var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken);
        if (slo == null)
            return;

        var errorBudget = await _errorBudgetCalculator.CalculateAsync(sloId, cancellationToken);

        // Check if we should create a violation
        if (errorBudget.RemainingBudgetPercentage <= 0) {
            var existingViolation = await _violationRepository.GetActiveBySloIdAsync(sloId, cancellationToken);
            if (existingViolation == null) {
                var violation = new SloViolation {
                    Id = Guid.NewGuid(),
                    SloId = sloId,
                    TenantId = slo.TenantId,
                    StartedAt = DateTime.UtcNow,
                    ActualValue = errorBudget.ActualPercentage,
                    TargetValue = errorBudget.TargetPercentage,
                    Severity = errorBudget.RemainingBudgetPercentage <= -10 ? "Critical" : "Warning"
                };

                await _violationRepository.AddAsync(violation, cancellationToken);
                _logger.LogWarning("SLO violation created for {SloName}: Actual={Actual}%, Target={Target}%",
                    slo.Name, errorBudget.ActualPercentage, errorBudget.TargetPercentage);
            }
        }

        // Trigger alerts based on thresholds
        await _alertManager.CheckAndTriggerAlertsAsync(slo, errorBudget, cancellationToken);
    }

    public async Task<double> GetErrorBudgetBurnRateAsync(Guid sloId, TimeSpan window, CancellationToken cancellationToken = default) {
        var endDate = DateTime.UtcNow;
        var startDate = endDate - window;

        var slis = await _sliRepository.GetBySloIdAsync(sloId, startDate, endDate, cancellationToken);
        var totalRequests = slis.Count();
        var failedRequests = slis.Count(s => !s.IsSuccessful);

        if (totalRequests == 0)
            return 0;

        var errorRate = (failedRequests / (double)totalRequests) * 100;
        var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken);

        if (slo == null)
            return 0;

        var errorBudget = 100 - slo.TargetPercentage;
        return errorBudget > 0 ? errorRate / errorBudget : 0;
    }

    public async Task<IEnumerable<SloViolationDto>> GetActiveSloViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default) {
        var violations = await _violationRepository.GetActiveViolationsAsync(tenantId, cancellationToken);
        var dtos = new List<SloViolationDto>();

        foreach (var violation in violations) {
            var slo = await _sloRepository.GetByIdAsync(violation.SloId, cancellationToken);
            if (slo != null) {
                dtos.Add(new SloViolationDto(
                    violation.Id,
                    violation.SloId,
                    slo.Name,
                    violation.StartedAt,
                    violation.EndedAt,
                    violation.ActualValue,
                    violation.TargetValue,
                    violation.Severity.ToString(),
                    violation.Notes
                ));
            }
        }

        return dtos;
    }

    public async Task<SloComplianceReportDto> GenerateComplianceReportAsync(Guid? tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default) {
        var slos = await _sloRepository.GetAllAsync(tenantId, cancellationToken);
        var summaries = new List<SloComplianceSummaryDto>();
        var compliantCount = 0;
        var violatedCount = 0;

        foreach (var slo in slos) {
            var compliance = await GetComplianceStatusAsync(slo.Id, startDate, endDate, cancellationToken);

            summaries.Add(new SloComplianceSummaryDto(
                slo.Id,
                slo.Name,
                slo.ServiceName,
                compliance.IsCompliant,
                compliance.ActualPercentage,
                compliance.TargetPercentage,
                compliance.ViolationCount
            ));

            if (compliance.IsCompliant)
                compliantCount++;
            else
                violatedCount++;
        }

        var overallCompliance = slos.Any() ? (compliantCount / (double)slos.Count()) * 100 : 100;

        return new SloComplianceReportDto(
            DateTime.UtcNow,
            startDate,
            endDate,
            slos.Count(),
            compliantCount,
            violatedCount,
            overallCompliance,
            summaries
        );
    }
}
