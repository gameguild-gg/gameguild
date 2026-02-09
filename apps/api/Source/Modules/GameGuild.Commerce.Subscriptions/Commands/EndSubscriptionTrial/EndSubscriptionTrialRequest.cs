namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for ending a subscription trial
/// </summary>
public sealed record EndSubscriptionTrialRequest(bool ConvertToPaid);
