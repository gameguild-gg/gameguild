namespace GameGuild.Modules.Billing.Models;

/// <summary>
///     Payment webhook payload
/// </summary>
public class PaymentWebhookPayload
{
    [Required]
    public string PaymentId { get; set; } = string.Empty;

    [Required]
    public string ExternalSubscriptionId { get; set; } = string.Empty;

    [Required]
    public Guid TenantId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }

    public string? FailureReason { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}

