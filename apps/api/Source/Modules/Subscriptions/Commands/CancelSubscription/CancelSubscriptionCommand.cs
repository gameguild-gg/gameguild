using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel a subscription
/// </summary>
public record CancelSubscriptionCommand(
    Guid SubscriptionId,
    CancellationReason Reason,
    string? Note = null,
    DateTime? EffectiveDate = null
) : ICommand;
