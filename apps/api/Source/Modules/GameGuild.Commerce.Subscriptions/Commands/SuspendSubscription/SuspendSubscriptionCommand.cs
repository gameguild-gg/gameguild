using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to suspend a subscription
/// </summary>
public record SuspendSubscriptionCommand(Guid SubscriptionId, string? Reason = null) : ICommand;
