namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for suspending a subscription
/// </summary>
public sealed record SuspendSubscriptionRequest(string? Reason);
