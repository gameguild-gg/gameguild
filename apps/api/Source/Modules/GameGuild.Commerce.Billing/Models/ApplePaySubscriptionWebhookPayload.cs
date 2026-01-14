namespace GameGuild.Commerce.Billing;

/// <summary>
///     Apple Pay (App Store Server Notifications) subscription webhook payload.
///     Concrete implementation of the abstract SubscriptionWebhookPayload.
/// </summary>
public sealed class ApplePaySubscriptionWebhookPayload : SubscriptionWebhookPayload
{
    /// <summary>
    ///     Apple Original Transaction ID
    /// </summary>
    public string? OriginalTransactionId { get; set; }

    /// <summary>
    ///     Apple Product ID
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    ///     Apple Bundle ID
    /// </summary>
    public string? BundleId { get; set; }

    /// <summary>
    ///     App Store environment (Production, Sandbox)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    ///     Subscription renewal date
    /// </summary>
    public DateTime? ExpiresDate { get; set; }

    /// <summary>
    ///     Whether subscription auto-renews
    /// </summary>
    public bool? IsAutoRenewing { get; set; }

    /// <summary>
    ///     Subscription group identifier
    /// </summary>
    public string? SubscriptionGroupIdentifier { get; set; }
}
