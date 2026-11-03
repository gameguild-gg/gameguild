using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to delete a subscription. Hard-deleting a subscription is a destructive
///     monetization operation and requires the monetization configure permission on every
///     dispatch path (issue #1205 G-9).
/// </summary>
[AuthorizeRequest(MonetizationPermission.Keys.Configure)]
public sealed record DeleteSubscriptionCommand(Guid SubscriptionId) : ICommand;
