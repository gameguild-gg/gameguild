using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to partially update a subscription
/// </summary>
public record PatchSubscriptionCommand(
    Guid SubscriptionId,
    BillingCycle? BillingCycle = null,
    bool? AutoRenew = null,
    string? ExternalSubscriptionId = null,
    string? ExternalCustomerId = null,
    string? Metadata = null) : ICommand;
