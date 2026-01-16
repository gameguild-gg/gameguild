using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to delete a subscription
/// </summary>
public record DeleteSubscriptionCommand(Guid SubscriptionId) : ICommand;
