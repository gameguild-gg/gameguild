namespace GameGuild.Commerce.Billing;

/// <summary>
///     Webhook processing configuration
/// </summary>
public class WebhookSettings
{
    /// <summary>
    ///     Maximum number of retry attempts for failed webhooks
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    ///     Timeout for webhook processing in seconds
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 30;

    /// <summary>
    ///     Whether to verify webhook signatures
    /// </summary>
    public bool VerifySignatures { get; set; } = true;

    /// <summary>
    ///     Whether to store webhook payloads in database
    /// </summary>
    public bool StorePayloads { get; set; } = true;
}
