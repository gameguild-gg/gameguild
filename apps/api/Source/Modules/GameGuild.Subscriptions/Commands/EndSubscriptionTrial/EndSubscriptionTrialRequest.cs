namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for ending a subscription trial
/// </summary>
public record EndSubscriptionTrialRequest(bool ConvertToPaid);
