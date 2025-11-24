using GameGuild.CQRS;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to cancel a subscription
/// </summary>
public record CancelSubscriptionCommand(Guid SubscriptionId, CancellationReason Reason, string? Note = null, DateTime? EffectiveDate = null) : ICommand;
