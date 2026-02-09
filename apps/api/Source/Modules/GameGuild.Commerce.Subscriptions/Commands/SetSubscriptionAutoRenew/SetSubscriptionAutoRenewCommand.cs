using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to set auto-renewal preference
/// </summary>
public sealed record SetSubscriptionAutoRenewCommand(Guid SubscriptionId, bool AutoRenew) : ICommand;
