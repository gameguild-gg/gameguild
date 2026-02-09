namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for starting a subscription trial
/// </summary>
public sealed record StartSubscriptionTrialRequest(int TrialDays);
