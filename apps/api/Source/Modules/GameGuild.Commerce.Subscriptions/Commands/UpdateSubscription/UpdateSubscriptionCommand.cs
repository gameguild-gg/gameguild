using GameGuild.CQRS;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to fully update a subscription
/// </summary>
public record UpdateSubscriptionCommand(
    Guid SubscriptionId,
    Guid PlanId,
    BillingCycle BillingCycle,
    decimal Amount,
    bool AutoRenew,
    string? ExternalSubscriptionId = null,
    string? ExternalCustomerId = null) : ICommand;
