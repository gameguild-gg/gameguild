namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for canceling a subscription
/// </summary>
public sealed record CancelSubscriptionRequest(string Reason, string? Note, DateTime? EffectiveDate);
