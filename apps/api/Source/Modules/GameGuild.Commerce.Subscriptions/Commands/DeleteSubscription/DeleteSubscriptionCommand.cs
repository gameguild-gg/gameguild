using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to delete a subscription
/// </summary>
public sealed record DeleteSubscriptionCommand(Guid SubscriptionId) : ICommand;
