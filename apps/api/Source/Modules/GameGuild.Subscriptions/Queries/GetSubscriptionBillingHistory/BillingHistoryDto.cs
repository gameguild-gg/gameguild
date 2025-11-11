namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     DTO for billing history information
/// </summary>
public abstract class BillingHistoryDto
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    public DateTime BillingDate { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public string Status { get; set; } = string.Empty;

    public string? ExternalPaymentId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
