using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to fully update a subscription. Administrative mutation of billing
///     state requires the monetization configure permission on every dispatch path
///     (issue #1205 G-9).
/// </summary>
[AuthorizeRequest(MonetizationPermission.Keys.Configure)]
public sealed record UpdateSubscriptionCommand(
    Guid SubscriptionId,
    Guid PlanId,
    BillingCycle BillingCycle,
    decimal Amount,
    bool AutoRenew,
    string? ExternalSubscriptionId = null,
    string? ExternalCustomerId = null) : ICommand;
