using GameGuild.Shared;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Services;

/// <summary>
///     Domain service for subscription and billing operations
/// </summary>
public interface ISubscriptionDomainService
{
    /// <summary>
    ///     Creates a new subscription for a tenant
    /// </summary>
    Task<Subscription> CreateSubscriptionAsync(
        Guid tenantId,
        Guid planId,
        BillingCycle billingCycle,
        Money amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculates pricing for a plan and billing cycle
    /// </summary>
    Task<PricingCalculationResult> CalculatePricingAsync(
        Guid planId,
        BillingCycle billingCycle,
        Dictionary<string, int>? addOns = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Processes subscription renewal
    /// </summary>
    Task<SubscriptionRenewalResult> ProcessRenewalAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancels a subscription with specified reason
    /// </summary>
    Task<Subscription> CancelSubscriptionAsync(
        Guid subscriptionId,
        CancellationReason reason,
        string? customReason = null,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Upgrades subscription to a new plan
    /// </summary>
    Task<SubscriptionUpgradeResult> UpgradeSubscriptionAsync(
        Guid subscriptionId,
        Guid newPlanId,
        bool prorated = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downgrades subscription to a new plan
    /// </summary>
    Task<SubscriptionDowngradeResult> DowngradeSubscriptionAsync(
        Guid subscriptionId,
        Guid newPlanId,
        bool immediate = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Processes subscription payment
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(
        Guid subscriptionId,
        Money amount,
        string paymentMethodId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Handles failed payment retry logic
    /// </summary>
    Task<PaymentRetryResult> RetryFailedPaymentAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates subscription state transition
    /// </summary>
    Task<bool> CanTransitionToStateAsync(Guid subscriptionId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default);
}

