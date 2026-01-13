using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to activate a subscription
/// </summary>
public record ActivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
