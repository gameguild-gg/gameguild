


namespace GameGuild.Monitoring.SLA;

public class ErrorBudgetCalculator(IServiceLevelIndicatorRepository sliRepository, IServiceLevelObjectiveRepository sloRepository) : IErrorBudgetCalculator
{
    public async Task<ErrorBudgetDto> CalculateAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID {sloId} not found"); }

        var startTime = DateTimeOffset.UtcNow.AddDays(-slo.TimeWindowDays);
        var endTime = DateTimeOffset.UtcNow;

        return await CalculateForPeriodAsync(sloId, startTime, endTime, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ErrorBudgetDto> CalculateForPeriodAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        var slo = await sloRepository.GetByIdAsync(sloId, cancellationToken).ConfigureAwait(false);

        if (slo == null) { throw new InvalidOperationException($"SLO with ID {sloId} not found"); }

        var indicators = await sliRepository.GetBySloIdAsync(sloId, cancellationToken).ConfigureAwait(false);
        var filteredIndicators = indicators.Where(i => i.Timestamp >= startTime && i.Timestamp <= endTime).ToList();

        var totalRequests = filteredIndicators.Count;
        var successfulRequests = filteredIndicators.Count(i => i.IsSuccessful);
        var failedRequests = totalRequests - successfulRequests;

        // Calculate actual percentage
        var actualPercentage = totalRequests > 0 ? successfulRequests / (double) totalRequests * 100.0 : 100.0;

        // Calculate error budget
        var allowedFailures = (long) Math.Floor(totalRequests * (slo.ErrorBudgetPercentage / 100.0));
        var remainingBudget = Math.Max(0, allowedFailures - failedRequests);
        var remainingBudgetPercentage = allowedFailures > 0 ? remainingBudget / (double) allowedFailures * 100.0 : 0.0;

        // Calculate burn rate (failures per day)
        var periodDays = (endTime - startTime).TotalDays;
        var burnRate = periodDays > 0 ? failedRequests / periodDays : 0;

        // Calculate time to exhaustion in hours
        double? timeToExhaustionHours = null;

        if (burnRate > 0 && remainingBudget > 0)
        {
            var daysToExhaustion = remainingBudget / burnRate;
            timeToExhaustionHours = daysToExhaustion * 24;
        }

        return new ErrorBudgetDto
        {
            ServiceLevelObjectiveId = slo.Id,
            TargetPercentage = slo.TargetPercentage,
            ErrorBudgetPercentage = slo.ErrorBudgetPercentage,
            ActualPercentage = actualPercentage,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            FailedRequests = failedRequests,
            AllowedFailures = allowedFailures,
            RemainingBudget = remainingBudget,
            RemainingBudgetPercentage = remainingBudgetPercentage,
            BurnRate = burnRate,
            TimeToExhaustionHours = timeToExhaustionHours,
            TimeWindowDays = slo.TimeWindowDays,
            WindowStart = startTime,
            WindowEnd = endTime,
            IsHealthy = actualPercentage >= slo.TargetPercentage && remainingBudgetPercentage > 0
        };
    }
}
