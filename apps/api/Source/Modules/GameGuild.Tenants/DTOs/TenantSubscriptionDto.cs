namespace GameGuild.Tenants.DTOs;

/// <summary>
///     DTO for tenant subscription information
/// </summary>
public abstract class TenantSubscriptionDto
{
    public Guid Id { get; set; }

    public Guid SubscriptionPlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime NextBillingDate { get; set; }

    public decimal CurrentPrice { get; set; }
}
