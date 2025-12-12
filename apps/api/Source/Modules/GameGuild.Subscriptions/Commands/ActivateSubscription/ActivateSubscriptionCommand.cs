using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to activate a subscription
/// </summary>
public record ActivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
