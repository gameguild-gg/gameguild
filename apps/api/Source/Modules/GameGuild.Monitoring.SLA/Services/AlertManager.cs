
using GameGuild.Notifications;
using GameGuild.Notifications.Services;

namespace GameGuild.Monitoring.SLA;

public class AlertManager(
    ISloViolationRepository violationRepository,
    IErrorBudgetCalculator errorBudgetCalculator,
    INotificationService notificationService) : IAlertManager
{

    public async Task<bool> CheckAndTriggerAlertAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        var alertTriggered = false;

        // Calculate error budget
        var errorBudget = await errorBudgetCalculator.CalculateAsync(slo.Id, cancellationToken).ConfigureAwait(false);

        // Check if SLO target is breached
        if (errorBudget.ActualPercentage < slo.TargetPercentage)
        {
            await HandleSloBreachAsync(slo, errorBudget.ActualPercentage, cancellationToken).ConfigureAwait(false);
            alertTriggered = true;
        }

        // Check if alert threshold is reached
        if (slo.ShouldTriggerAlert() && errorBudget.RemainingBudgetPercentage <= slo.AlertThresholdPercentage)
        {
            await SendErrorBudgetAlertAsync(slo, errorBudget.RemainingBudgetPercentage, cancellationToken).ConfigureAwait(false);
            alertTriggered = true;
        }

        // Check for high burn rate
        if (errorBudget.BurnRate > 0 && errorBudget.TimeToExhaustionHours.HasValue)
        {
            if (errorBudget.TimeToExhaustionHours.Value < 24) // Less than 1 day
            {
                await SendErrorBudgetAlertAsync(slo, errorBudget.RemainingBudgetPercentage, cancellationToken).ConfigureAwait(false);
                alertTriggered = true;
            }
        }

        return alertTriggered;
    }

    public async Task<bool> SendViolationAlertAsync(SloViolation violation, CancellationToken cancellationToken = default)
    {
        var result = await notificationService.SendAsync(
            recipientId: Guid.Empty, // System-level alert — routed to admin channel
            type: NotificationType.System,
            title: $"SLO Violation: {violation.Description}",
            message: $"SLO target breached. Actual: {violation.ActualValue:F2}%, Target: {violation.TargetValue:F2}%. Severity: {violation.Severity}. Started at: {violation.StartedAt:u}",
            channel: NotificationChannel.InApp,
            priority: NotificationPriority.High,
            referenceEntityId: violation.Id,
            referenceEntityType: "SloViolation",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.IsSuccess;
    }

    public async Task<bool> SendErrorBudgetAlertAsync(ServiceLevelObjective slo, double remainingBudgetPercentage, CancellationToken cancellationToken = default)
    {
        var result = await notificationService.SendAsync(
            recipientId: Guid.Empty, // System-level alert — routed to admin channel
            type: NotificationType.System,
            title: $"Error Budget Alert: {slo.Name}",
            message: $"Service '{slo.ServiceName}' error budget at {remainingBudgetPercentage:F1}% remaining (threshold: {slo.AlertThresholdPercentage:F1}%).",
            channel: NotificationChannel.InApp,
            priority: NotificationPriority.High,
            referenceEntityId: slo.Id,
            referenceEntityType: "ServiceLevelObjective",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.IsSuccess;
    }

    private async Task HandleSloBreachAsync(ServiceLevelObjective slo, double actualPercentage, CancellationToken cancellationToken)
    {
        // Check if there's already an ongoing violation
        var ongoingViolations = await violationRepository.GetOngoingViolationsAsync(slo.Id, cancellationToken).ConfigureAwait(false);

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
            await violationRepository.AddAsync(violation, cancellationToken).ConfigureAwait(false);
            await SendViolationAlertAsync(violation, cancellationToken).ConfigureAwait(false);
        }
    }
}
