namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for ending a subscription trial
/// </summary>
public record EndSubscriptionTrialRequest(bool ConvertToPaid);
