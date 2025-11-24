using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to suspend a subscription
/// </summary>
public record SuspendSubscriptionCommand(Guid SubscriptionId, string? Reason = null) : ICommand;
