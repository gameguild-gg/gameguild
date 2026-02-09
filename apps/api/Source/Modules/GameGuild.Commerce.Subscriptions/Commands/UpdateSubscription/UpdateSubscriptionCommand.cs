using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to fully update a subscription
/// </summary>
public sealed record UpdateSubscriptionCommand(
    Guid SubscriptionId,
    Guid PlanId,
    BillingCycle BillingCycle,
    decimal Amount,
    bool AutoRenew,
    string? ExternalSubscriptionId = null,
    string? ExternalCustomerId = null) : ICommand;
