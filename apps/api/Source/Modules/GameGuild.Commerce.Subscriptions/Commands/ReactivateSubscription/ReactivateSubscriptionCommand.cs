using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to reactivate a suspended subscription
/// </summary>
public sealed record ReactivateSubscriptionCommand(Guid SubscriptionId) : ICommand;
