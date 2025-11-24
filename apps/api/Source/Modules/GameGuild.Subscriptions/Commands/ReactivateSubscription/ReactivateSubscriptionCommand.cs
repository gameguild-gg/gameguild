using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to reactivate a suspended subscription
/// </summary>
public record ReactivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
