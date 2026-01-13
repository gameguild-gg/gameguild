using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     DTO for creating a new subscription
/// </summary>
public record CreateSubscriptionDto
{
    /// <summary>
    ///     Plan ID for the subscription
    /// </summary>
    public Guid PlanId { get; init; }

    /// <summary>
    ///     Billing cycle frequency
    /// </summary>
    public BillingCycle BillingCycle { get; init; }

    /// <summary>
    ///     Optional trial end date
    /// </summary>
    public DateTime? TrialEndDate { get; init; }

    /// <summary>
    ///     External subscription ID from payment provider (Stripe, PayPal, etc.)
    /// </summary>
    public string? ExternalSubscriptionId { get; init; }

    /// <summary>
    ///     External customer ID from payment provider
    /// </summary>
    public string? ExternalCustomerId { get; init; }

    /// <summary>
    ///     Additional metadata as JSON string
    /// </summary>
    public string? Metadata { get; init; }
}
