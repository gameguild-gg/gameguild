using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to reactivate a suspended subscription
/// </summary>
public record ReactivateSubscriptionCommand(Guid SubscriptionId) : ICommand;

