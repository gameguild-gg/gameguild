using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Models;

namespace GameGuild.Monitoring.SLA.Services;

public class AlertManager(ISloViolationRepository violationRepository, IErrorBudgetCalculator errorBudgetCalculator) : IAlertManager
{
    // TODO: Inject INotificationService when Notifications module is integrated

    public async Task<bool> CheckAndTriggerAlertAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        var alertTriggered = false;

        // Calculate error budget
        var errorBudget = await errorBudgetCalculator.CalculateAsync(slo.Id, cancellationToken);

        // Check if SLO target is breached
        if (errorBudget.ActualPercentage < slo.TargetPercentage)
        {
            await HandleSloBreachAsync(slo, errorBudget.ActualPercentage, cancellationToken);
            alertTriggered = true;
        }

        // Check if alert threshold is reached
        if (slo.ShouldTriggerAlert() && errorBudget.RemainingBudgetPercentage <= slo.AlertThresholdPercentage)
        {
            await SendErrorBudgetAlertAsync(slo, errorBudget.RemainingBudgetPercentage, cancellationToken);
            alertTriggered = true;
        }

        // Check for high burn rate
        if (errorBudget.BurnRate > 0 && errorBudget.TimeToExhaustionHours.HasValue)
        {
            if (errorBudget.TimeToExhaustionHours.Value < 24) // Less than 1 day
            {
                await SendErrorBudgetAlertAsync(slo, errorBudget.RemainingBudgetPercentage, cancellationToken);
                alertTriggered = true;
            }
        }

        return alertTriggered;
    }

    public async Task<bool> SendViolationAlertAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate with Notifications module
        // await _notificationService.SendAlertAsync(new ViolationAlert
        // {
        //     SloId = violation.ServiceLevelObjectiveId,
        //     ViolationId = violation.Id,
        //     Severity = violation.Severity,
        //     StartedAt = violation.StartedAt,
        //     ActualValue = violation.ActualValue,
        //     TargetValue = violation.TargetValue
        // }, cancellationToken);

        await Task.CompletedTask;

        return true; // TODO: Return actual result from notification service
    }

    public async Task<bool> SendErrorBudgetAlertAsync(ServiceLevelObjective slo, double remainingBudgetPercentage, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate with Notifications module
        // await _notificationService.SendAlertAsync(new ErrorBudgetAlert
        // {
        //     SloId = slo.Id,
        //     SloName = slo.Name,
        //     ServiceName = slo.ServiceName,
        //     ErrorBudgetRemaining = remainingBudgetPercentage,
        //     AlertThreshold = slo.AlertThresholdPercentage
        // }, cancellationToken);

        await Task.CompletedTask;

        return true; // TODO: Return actual result from notification service
    }

    private async Task HandleSloBreachAsync(ServiceLevelObjective slo, double actualPercentage, CancellationToken cancellationToken)
    {
        // Check if there's already an ongoing violation
        var ongoingViolations = await violationRepository.GetOngoingViolationsAsync(slo.Id, cancellationToken);

        if (ongoingViolations.Count == 0)
        {
            // Create new violation
            var violation = new SloViolation
            {
                ServiceLevelObjectiveId = slo.Id,
                StartedAt = DateTimeOffset.UtcNow,
                ActualValue = actualPercentage,
                TargetValue = slo.TargetPercentage,
                Severity = SloViolation.DetermineSeverity(actualPercentage, slo.TargetPercentage),
                Description = $"SLO '{slo.Name}' breached. Actual: {actualPercentage:F2}%, Target: {slo.TargetPercentage:F2}%"
            };

            // Set TenantId using SetProperties
            violation.SetProperties(new Dictionary<string, object?> { { "TenantId", slo.TenantId } });

            violation.TriggerAlert();
            await violationRepository.AddAsync(violation, cancellationToken);
            await SendViolationAlertAsync(violation, cancellationToken);
        }
    }
}
