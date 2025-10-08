namespace Subscriptions.Domain.SubscriptionPlans.Models;

/// <summary>
///     DTO for subscription plan information
/// </summary>
public class SubscriptionPlanDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ExternalId { get; set; }

    public long MonthlyPriceInCents { get; set; }

    public long? AnnualPriceInCents { get; set; }

    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public int? MaxUsers { get; set; }

    public long? MaxStorageMb { get; set; }

    public long? MaxApiCallsPerMonth { get; set; }

    public bool HasPrioritySupport { get; set; }

    public bool HasAdvancedAnalytics { get; set; }

    public bool HasCustomBranding { get; set; }

    public string? Features { get; set; }

    public int TrialPeriodDays { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int SubscriptionsCount { get; set; }
}

