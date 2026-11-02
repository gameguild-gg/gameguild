namespace GameGuild.Commerce.Billing;

/// <summary>
///     DTO for webhook event response
/// </summary>
public class BillingWebhookEventDto
{
    public Guid Id { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ExternalEventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public bool IsProcessed { get; set; }

    public bool IsFailed { get; set; }

    public int ProcessingAttempts { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? SubscriptionId { get; set; }

    /// <summary>Source IP address the delivery was accepted from (issue #408).</summary>
    public string? SourceIpAddress { get; set; }

    /// <summary>Bounded user agent of the delivery (issue #408).</summary>
    public string? UserAgent { get; set; }
}
