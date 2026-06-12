using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Default implementation of subscription notification service.
///     Logs operational telemetry and publishes in-app billing notifications through the shared notification contract.
/// </summary>
public class SubscriptionNotificationService : ISubscriptionNotificationService
{
    private readonly ILogger<SubscriptionNotificationService> _logger;
    private readonly ISubscriptionPlanService _planService;
    private readonly IApplicationNotificationPublisher? _notificationPublisher;

    public SubscriptionNotificationService(
        ILogger<SubscriptionNotificationService> logger,
        ISubscriptionPlanService planService,
        IApplicationNotificationPublisher? notificationPublisher = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
        _notificationPublisher = notificationPublisher;
    }

    public async Task SendRenewalReminderAsync(Subscription subscription, int daysUntilRenewal, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Renewal reminder: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' will renew in {DaysUntilRenewal} days on {RenewalDate:yyyy-MM-dd}. " +
            "Amount: {Amount} {Currency}",
            subscription.Id,
            subscription.TenantId,
            plan,
            daysUntilRenewal,
            subscription.CurrentPeriodEnd,
            subscription.Amount.Amount,
            subscription.Amount.Currency);

        await PublishBillingNotificationAsync(
            subscription,
            "Subscription renewal reminder",
            $"Your {plan} subscription renews in {daysUntilRenewal} days.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "renewal-reminder", ["daysUntilRenewal"] = daysUntilRenewal.ToString() })
            .ConfigureAwait(false);
    }

    public async Task SendTrialExpirationReminderAsync(Subscription subscription, int daysUntilExpiration, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Trial expiration reminder: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' trial expires in {DaysUntilExpiration} days on {ExpirationDate:yyyy-MM-dd}. " +
            "To continue using the service, please add a payment method.",
            subscription.Id,
            subscription.TenantId,
            plan,
            daysUntilExpiration,
            subscription.TrialEndDate);

        await PublishBillingNotificationAsync(
            subscription,
            "Trial expiration reminder",
            $"Your {plan} trial expires in {daysUntilExpiration} days. Add a payment method to keep access active.",
            "High",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "trial-expiration", ["daysUntilExpiration"] = daysUntilExpiration.ToString() })
            .ConfigureAwait(false);
    }

    public async Task SendPaymentFailureNotificationAsync(Subscription subscription, string failureReason, int retryAttempt, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogWarning(
            "[NOTIFICATION] Payment failure: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' failed payment attempt {RetryAttempt}. Reason: {FailureReason}. " +
            "Amount: {Amount} {Currency}. Please update payment method to avoid service interruption.",
            subscription.Id,
            subscription.TenantId,
            plan,
            retryAttempt,
            failureReason,
            subscription.Amount.Amount,
            subscription.Amount.Currency);

        await PublishBillingNotificationAsync(
            subscription,
            "Payment failed",
            $"Payment attempt {retryAttempt} failed for your {plan} subscription: {failureReason}.",
            "Urgent",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "payment-failure", ["retryAttempt"] = retryAttempt.ToString(), ["failureReason"] = failureReason })
            .ConfigureAwait(false);
    }

    public async Task SendSubscriptionActivatedNotificationAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Subscription activated: Subscription {SubscriptionId} for tenant {TenantId} " +
            "is now active on plan '{PlanName}'. Billing cycle: {BillingCycle}. " +
            "Next billing date: {NextBillingDate:yyyy-MM-dd}. Amount: {Amount} {Currency}",
            subscription.Id,
            subscription.TenantId,
            plan,
            subscription.BillingCycle,
            subscription.CurrentPeriodEnd,
            subscription.Amount.Amount,
            subscription.Amount.Currency);

        await PublishBillingNotificationAsync(
            subscription,
            "Subscription activated",
            $"Your {plan} subscription is active.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "activated" })
            .ConfigureAwait(false);
    }

    public async Task SendSubscriptionCancelledNotificationAsync(Subscription subscription, CancellationReason cancellationReason, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Subscription cancelled: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' has been cancelled. Reason: {CancellationReason}. " +
            "Access continues until: {AccessEndDate:yyyy-MM-dd}",
            subscription.Id,
            subscription.TenantId,
            plan,
            cancellationReason,
            subscription.CancelledAt ?? subscription.CurrentPeriodEnd);

        await PublishBillingNotificationAsync(
            subscription,
            "Subscription cancelled",
            $"Your {plan} subscription was cancelled. Access continues until {(subscription.CancelledAt ?? subscription.CurrentPeriodEnd):yyyy-MM-dd}.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "cancelled", ["reason"] = cancellationReason.ToString() })
            .ConfigureAwait(false);
    }

    public async Task SendSubscriptionSuspendedNotificationAsync(Subscription subscription, string? suspensionReason, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogWarning(
            "[NOTIFICATION] Subscription suspended: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' has been suspended. Reason: {SuspensionReason}. " +
            "Please resolve the issue to restore access.",
            subscription.Id,
            subscription.TenantId,
            plan,
            suspensionReason ?? "Payment failure");

        await PublishBillingNotificationAsync(
            subscription,
            "Subscription suspended",
            $"Your {plan} subscription was suspended: {suspensionReason ?? "Payment failure"}.",
            "Urgent",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "suspended", ["reason"] = suspensionReason ?? "Payment failure" })
            .ConfigureAwait(false);
    }

    public async Task SendSubscriptionReactivatedNotificationAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanNameAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Subscription reactivated: Subscription {SubscriptionId} for tenant {TenantId} " +
            "on plan '{PlanName}' has been reactivated. Access has been restored.",
            subscription.Id,
            subscription.TenantId,
            plan);

        await PublishBillingNotificationAsync(
            subscription,
            "Subscription reactivated",
            $"Your {plan} subscription has been reactivated.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "reactivated" })
            .ConfigureAwait(false);
    }

    public async Task SendPlanUpgradeNotificationAsync(Subscription subscription, Guid oldPlanId, Guid newPlanId, CancellationToken cancellationToken = default)
    {
        var oldPlan = await GetPlanNameAsync(oldPlanId, cancellationToken).ConfigureAwait(false);
        var newPlan = await GetPlanNameAsync(newPlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Plan upgrade: Subscription {SubscriptionId} for tenant {TenantId} " +
            "upgraded from '{OldPlan}' to '{NewPlan}'. New features are now available. " +
            "New amount: {Amount} {Currency}",
            subscription.Id,
            subscription.TenantId,
            oldPlan,
            newPlan,
            subscription.Amount.Amount,
            subscription.Amount.Currency);

        await PublishBillingNotificationAsync(
            subscription,
            "Plan upgraded",
            $"Your subscription was upgraded from {oldPlan} to {newPlan}.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "plan-upgraded", ["oldPlan"] = oldPlan, ["newPlan"] = newPlan })
            .ConfigureAwait(false);
    }

    public async Task SendPlanDowngradeNotificationAsync(Subscription subscription, Guid oldPlanId, Guid newPlanId, DateTime effectiveDate, CancellationToken cancellationToken = default)
    {
        var oldPlan = await GetPlanNameAsync(oldPlanId, cancellationToken).ConfigureAwait(false);
        var newPlan = await GetPlanNameAsync(newPlanId, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation(
            "[NOTIFICATION] Plan downgrade: Subscription {SubscriptionId} for tenant {TenantId} " +
            "scheduled downgrade from '{OldPlan}' to '{NewPlan}'. Effective date: {EffectiveDate:yyyy-MM-dd}. " +
            "New amount: {Amount} {Currency}",
            subscription.Id,
            subscription.TenantId,
            oldPlan,
            newPlan,
            effectiveDate,
            subscription.Amount.Amount,
            subscription.Amount.Currency);

        await PublishBillingNotificationAsync(
            subscription,
            "Plan downgrade scheduled",
            $"Your subscription downgrade from {oldPlan} to {newPlan} is scheduled for {effectiveDate:yyyy-MM-dd}.",
            "Normal",
            cancellationToken,
            new Dictionary<string, string> { ["event"] = "plan-downgraded", ["oldPlan"] = oldPlan, ["newPlan"] = newPlan, ["effectiveDate"] = effectiveDate.ToString("O") })
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the plan name for logging purposes.
    /// </summary>
    private async Task<string> GetPlanNameAsync(Guid planId, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _planService.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
            return plan?.Name ?? $"Plan {planId}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve plan name for {PlanId}", planId);
            return $"Plan {planId}";
        }
    }

    private async Task PublishBillingNotificationAsync(
        Subscription subscription,
        string title,
        string message,
        string priority,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (_notificationPublisher == null)
        {
            return;
        }

        var result = await _notificationPublisher.PublishAsync(
                new ApplicationNotificationMessage(
                    subscription.CreatedByUserId,
                    title,
                    message,
                    "Billing",
                    priority,
                    subscription.TenantId,
                    $"/billing/subscriptions/{subscription.Id}",
                    subscription.Id,
                    nameof(Subscription),
                    metadata),
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Subscription notification publish failed for subscription {SubscriptionId}: {Error}",
                subscription.Id,
                result.ErrorMessage);
        }
    }
}
