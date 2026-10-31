namespace GameGuild.Commerce.Billing;

/// <summary>
///     Decoded Braintree webhook notification (issue #408). Carries only the routing-relevant
///     projection of the provider payload: event kind, timestamp and the bounded subject
///     identifiers — never raw XML, tokens or PAN data.
/// </summary>
public sealed class BraintreeWebhookNotification
{
    /// <summary>Provider event kind, e.g. <c>subscription_charged_successfully</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Provider timestamp of the notification, when present.</summary>
    public DateTime? TimestampUtc { get; init; }

    /// <summary>Subject subscription id (subscription and transaction-bearing kinds).</summary>
    public string? SubscriptionId { get; init; }

    /// <summary>Subject subscription status as reported by the provider.</summary>
    public string? SubscriptionStatus { get; init; }

    /// <summary>Subject transaction id (transaction-bearing kinds).</summary>
    public string? TransactionId { get; init; }

    /// <summary>Subject transaction status as reported by the provider.</summary>
    public string? TransactionStatus { get; init; }

    /// <summary>Monetary amount on the subject, when present.</summary>
    public decimal? Amount { get; init; }

    /// <summary>ISO 4217 currency of <see cref="Amount"/>, when present.</summary>
    public string? CurrencyIsoCode { get; init; }

    /// <summary>Subject dispute id (dispute kinds).</summary>
    public string? DisputeId { get; init; }

    /// <summary>Subject dispute status as reported by the provider.</summary>
    public string? DisputeStatus { get; init; }
}
