using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Default implementation of subscription notification service.
///     Logs all notifications and provides structured data for future notification integrations.
///     
///     Integration Points:
///     - When GameGuild.Notifications module is implemented, replace this with a real implementation
///     - Can integrate with email services (AWS SES, etc.)
///     - Can integrate with push notification services (Firebase, OneSignal, etc.)
///     - Can integrate with in-app notification systems
/// </summary>
public class SubscriptionNotificationService : ISubscriptionNotificationService
{
    private readonly ILogger<SubscriptionNotificationService> _logger;
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionNotificationService(
        ILogger<SubscriptionNotificationService> logger,
        ISubscriptionPlanService planService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
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

        // Future: Send email/push notification
        // await _emailService.SendTemplatedEmailAsync(
        //     template: "subscription-renewal-reminder",
        //     to: await GetTenantEmailAsync(subscription.TenantId),
        //     data: new { subscription, daysUntilRenewal, plan });
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

        // Future: Send email/push notification with CTA to add payment method
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

        // Future: Send urgent email with CTA to update payment method
        // Include retry information and potential service suspension warning
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

        // Future: Send welcome/confirmation email
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

        // Future: Send cancellation confirmation with feedback survey link
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

        // Future: Send urgent notification with steps to restore access
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

        // Future: Send reactivation confirmation
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

        // Future: Send upgrade confirmation with list of new features
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

        // Future: Send downgrade confirmation with information about feature changes
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
}
