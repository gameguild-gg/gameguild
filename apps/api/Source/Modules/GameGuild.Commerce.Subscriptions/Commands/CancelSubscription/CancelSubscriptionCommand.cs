using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to cancel a subscription
/// </summary>
public sealed record CancelSubscriptionCommand(Guid SubscriptionId, CancellationReason Reason, string? Note = null, DateTime? EffectiveDate = null) : ICommand;
