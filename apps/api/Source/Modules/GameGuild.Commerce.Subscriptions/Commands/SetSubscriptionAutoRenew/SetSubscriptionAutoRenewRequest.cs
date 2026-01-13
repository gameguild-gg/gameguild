namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for setting subscription auto-renew
/// </summary>
public record SetSubscriptionAutoRenewRequest(bool AutoRenew);
