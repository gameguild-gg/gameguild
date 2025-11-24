namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for canceling a subscription
/// </summary>
public record CancelSubscriptionRequest(string Reason, string? Note, DateTime? EffectiveDate);
