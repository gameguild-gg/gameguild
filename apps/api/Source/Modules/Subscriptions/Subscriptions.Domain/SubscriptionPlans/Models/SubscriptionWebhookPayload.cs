namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Models;

/// <summary>
///     Subscription webhook payload
/// </summary>
public class SubscriptionWebhookPayload
{
    [Required]
    public string ExternalSubscriptionId { get; set; } = string.Empty;

    [Required]
    public Guid TenantId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? NextBillingDate { get; set; }

    public Dictionary<string, object>? Metadata { get; set; }
}

