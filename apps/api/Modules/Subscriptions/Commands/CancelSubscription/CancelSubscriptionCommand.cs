using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Models;


namespace GameGuild.Modules.Subscriptions.Commands.CancelSubscription;

/// <summary>
/// Command to cancel an active subscription with optional scheduling
/// </summary>
/// <param name="SubscriptionId">The unique identifier of the subscription to cancel</param>
/// <param name="Reason">The reason for cancellation (e.g., user request, payment failure)</param>
/// <param name="Note">Optional cancellation note for record keeping</param>
/// <param name="EffectiveDate">Optional effective date for scheduled cancellation (default: immediate)</param>
public record CancelSubscriptionCommand(
  Guid SubscriptionId,
  CancellationReason Reason,
  string? Note = null,
  DateTime? EffectiveDate = null
) : ICommand;
