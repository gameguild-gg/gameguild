namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for suspending a subscription
/// </summary>
public record SuspendSubscriptionRequest(string? Reason);
