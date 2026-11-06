using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to partially update a subscription. Administrative mutation of billing
///     state requires the monetization configure permission on every dispatch path
///     (issue #1205 G-9).
/// </summary>
[AuthorizeRequest(MonetizationPermission.Keys.Configure)]
public sealed record PatchSubscriptionCommand(
    Guid SubscriptionId,
    BillingCycle? BillingCycle = null,
    bool? AutoRenew = null,
    string? ExternalSubscriptionId = null,
    string? ExternalCustomerId = null,
    string? Metadata = null) : ICommand;
