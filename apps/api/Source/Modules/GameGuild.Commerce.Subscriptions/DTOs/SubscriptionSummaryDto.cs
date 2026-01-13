
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     DTO for subscription summary information
/// </summary>
public record SubscriptionSummaryDto
{
    /// <summary>
    ///     Subscription ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    ///     Tenant ID
    /// </summary>
    public Guid TenantId { get; init; }

    /// <summary>
    ///     Plan ID
    /// </summary>
    public Guid PlanId { get; init; }

    /// <summary>
    ///     Plan name
    /// </summary>
    public string PlanName { get; init; } = string.Empty;

    /// <summary>
    ///     Current subscription status
    /// </summary>
    public SubscriptionStatus Status { get; init; }

    /// <summary>
    ///     Subscription amount
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    ///     Billing cycle
    /// </summary>
    public BillingCycle BillingCycle { get; init; }

    /// <summary>
    ///     Current billing period start date
    /// </summary>
    public DateTime CurrentPeriodStart { get; init; }

    /// <summary>
    ///     Current billing period end date
    /// </summary>
    public DateTime CurrentPeriodEnd { get; init; }

    /// <summary>
    ///     Next billing date
    /// </summary>
    public DateTime? NextBillingDate { get; init; }

    /// <summary>
    ///     Trial end date (if in trial)
    /// </summary>
    public DateTime? TrialEndDate { get; init; }

    /// <summary>
    ///     Remaining trial days (if in trial)
    /// </summary>
    public int? RemainingTrialDays { get; init; }

    /// <summary>
    ///     Days until next billing
    /// </summary>
    public int DaysUntilNextBilling { get; init; }

    /// <summary>
    ///     Whether subscription auto-renews
    /// </summary>
    public bool AutoRenew { get; init; }

    /// <summary>
    ///     Number of completed billing cycles
    /// </summary>
    public int BillingCycleCount { get; init; }

    /// <summary>
    ///     Last payment date
    /// </summary>
    public DateTime? LastPaymentAt { get; init; }

    /// <summary>
    ///     External subscription ID (from payment provider)
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    ///     Whether subscription is active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    ///     Whether subscription is in trial
    /// </summary>
    public bool IsTrialing { get; init; }
}
