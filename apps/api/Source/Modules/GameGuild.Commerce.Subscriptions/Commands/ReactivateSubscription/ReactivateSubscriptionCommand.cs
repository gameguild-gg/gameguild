using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to reactivate a suspended subscription
/// </summary>
public record ReactivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
