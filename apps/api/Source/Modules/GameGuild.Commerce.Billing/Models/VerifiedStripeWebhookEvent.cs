namespace GameGuild.Commerce.Billing;

/// <summary>
/// Authenticated Stripe event metadata. Instances are created only after signature and endpoint checks pass.
/// </summary>
public sealed record VerifiedStripeWebhookEvent
{
    public required string EventId { get; init; }
    public required string EventType { get; init; }
    public required string ProviderEnvironment { get; init; }
    public required string ProviderAccountId { get; init; }
    public required string WebhookEndpointId { get; init; }
    public required string EventSchemaVersion { get; init; }
    public required string ProviderObjectId { get; init; }
    public required string ProviderObjectType { get; init; }
    public required string ProviderMonetaryLeg { get; init; }
    public required string VerifiedPayload { get; init; }
    public required string RetainedPayload { get; init; }
    public required string PayloadSha256 { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public bool IsLiveMode { get; init; }
    public Guid? TenantId { get; init; }
    public string? ExternalSubscriptionId { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public decimal? CumulativeRefundedAmount { get; init; }
    public decimal? CumulativeDisputedAmount { get; init; }
}

public interface IStripeWebhookVerifier
{
    VerifiedStripeWebhookEvent Verify(string payload, string signature);
}

public interface IStripeVerifiedEventConsumer
{
    ValueTask<bool> TryConsumeAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken);
}
