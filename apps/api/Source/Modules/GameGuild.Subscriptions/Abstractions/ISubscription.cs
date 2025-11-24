using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Abstractions;

/// <summary>
///     Interface for subscription entities
/// </summary>
public interface ISubscription
{
    /// <summary>
    ///     Unique identifier
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Tenant ID
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    ///     Plan ID
    /// </summary>
    Guid PlanId { get; }

    /// <summary>
    ///     Subscription status
    /// </summary>
    SubscriptionStatus Status { get; }

    /// <summary>
    ///     Billing cycle
    /// </summary>
    BillingCycle BillingCycle { get; }

    /// <summary>
    ///     Subscription amount
    /// </summary>
    Money Amount { get; }

    /// <summary>
    ///     Start date
    /// </summary>
    DateTime StartDate { get; }

    /// <summary>
    ///     End date
    /// </summary>
    DateTime? EndDate { get; }

    /// <summary>
    ///     Next billing date
    /// </summary>
    DateTime NextBillingDate { get; }

    /// <summary>
    ///     Whether subscription is active
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    ///     Whether subscription is in trial
    /// </summary>
    bool IsTrialing { get; }

    /// <summary>
    ///     Whether subscription is cancelled
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    ///     Gets remaining trial days
    /// </summary>
    int? GetRemainingTrialDays();

    /// <summary>
    ///     Gets days until next billing
    /// </summary>
    int GetDaysUntilNextBilling();
}
