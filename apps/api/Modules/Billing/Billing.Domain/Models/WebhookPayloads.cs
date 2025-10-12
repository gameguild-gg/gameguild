namespace GameGuild.Modules.Billing.Models;

/// <summary>
/// Payload for subscription webhook events
/// </summary>
public class SubscriptionWebhookPayload
{
    public string ExternalSubscriptionId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
}

