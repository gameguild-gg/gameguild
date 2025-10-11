using GameGuild.Modules.SlaMonitoring.Repositories;


namespace GameGuild.Modules.SlaMonitoring.Services;

/// <summary>
/// Interface for error budget calculation.
/// </summary>
public interface IErrorBudgetCalculator
{
    Task<ErrorBudgetDto> CalculateAsync(Guid sloId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Calculates error budgets for SLOs.
/// </summary>
public class ErrorBudgetCalculator : IErrorBudgetCalculator
{
    private readonly IServiceLevelObjectiveRepository _sloRepository;
    private readonly IServiceLevelIndicatorRepository _sliRepository;
    private readonly ILogger<ErrorBudgetCalculator> _logger;

    public ErrorBudgetCalculator(
        IServiceLevelObjectiveRepository sloRepository,
        IServiceLevelIndicatorRepository sliRepository,
        ILogger<ErrorBudgetCalculator> logger)
    {
        _sloRepository = sloRepository;
        _sliRepository = sliRepository;
        _logger = logger;
    }

    public async Task<ErrorBudgetDto> CalculateAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        var slo = await _sloRepository.GetByIdAsync(sloId, cancellationToken);
        if (slo == null)
            throw new InvalidOperationException($"SLO {sloId} not found");

        var windowStart = DateTime.UtcNow.AddDays(-slo.TimeWindowDays);
        var windowEnd = DateTime.UtcNow;

        var slis = await _sliRepository.GetBySloIdAsync(sloId, windowStart, windowEnd, cancellationToken);

        var totalRequests = slis.Count();
        var successfulRequests = slis.Count(s => s.IsSuccessful);
        var failedRequests = totalRequests - successfulRequests;

        // Calculate actual percentage
        var actualPercentage = totalRequests > 0
            ? (successfulRequests / (double)totalRequests) * 100
            : 100;

        // Calculate error budget
        var errorBudgetPercentage = 100 - slo.TargetPercentage;
        var allowedFailures = (errorBudgetPercentage / 100.0) * totalRequests;
        var remainingBudget = allowedFailures - failedRequests;
        var remainingBudgetPercentage = allowedFailures > 0
            ? (remainingBudget / allowedFailures) * 100
            : 0;

        // Calculate burn rate (failures per day)
        var burnRate = slo.TimeWindowDays > 0
            ? failedRequests / (double)slo.TimeWindowDays
            : 0;

        // Estimate time to exhaustion
        var timeToExhaustion = burnRate > 0 && remainingBudget > 0
            ? TimeSpan.FromDays(remainingBudget / burnRate)
            : TimeSpan.MaxValue;

        _logger.LogDebug("Calculated error budget for SLO {SloId}: Remaining={Remaining}%, BurnRate={BurnRate}/day",
            sloId, remainingBudgetPercentage, burnRate);

        return new ErrorBudgetDto(
            sloId,
            slo.Name,
            slo.TargetPercentage,
            actualPercentage,
            remainingBudgetPercentage,
            burnRate,
            windowStart,
            windowEnd,
            totalRequests,
            failedRequests,
            timeToExhaustion
        );
    }
}
