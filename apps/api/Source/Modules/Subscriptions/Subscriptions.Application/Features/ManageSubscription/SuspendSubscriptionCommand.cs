using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to suspend a subscription
/// </summary>
public record SuspendSubscriptionCommand(
    Guid SubscriptionId,
    string? Reason = null
) : ICommand;

