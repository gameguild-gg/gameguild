namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Response model for subscription plans
/// </summary>
public class SubscriptionPlanResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Interval { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int MaxUsers { get; set; }

    public int MaxProjects { get; set; }

    public long MaxStorage { get; set; }

    public int MaxApiCallsPerMonth { get; set; }

    public bool HasAdvancedFeatures { get; set; }

    public bool HasPrioritySupport { get; set; }
}
