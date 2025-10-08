namespace GameGuild.Modules.Billing.DTOs;

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
}

