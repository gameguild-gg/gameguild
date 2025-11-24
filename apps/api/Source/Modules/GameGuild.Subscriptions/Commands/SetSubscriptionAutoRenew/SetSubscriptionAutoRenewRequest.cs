namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for setting subscription auto-renew
/// </summary>
public record SetSubscriptionAutoRenewRequest(bool AutoRenew);
