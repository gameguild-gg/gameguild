namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for canceling a subscription
/// </summary>
public record CancelSubscriptionRequest(string Reason, string? Note, DateTime? EffectiveDate);
