using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to activate a subscription
/// </summary>
public sealed record ActivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
