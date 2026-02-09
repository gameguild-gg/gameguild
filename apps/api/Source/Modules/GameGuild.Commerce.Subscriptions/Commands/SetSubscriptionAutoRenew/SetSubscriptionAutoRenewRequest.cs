namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for setting subscription auto-renew
/// </summary>
public sealed record SetSubscriptionAutoRenewRequest(bool AutoRenew);
