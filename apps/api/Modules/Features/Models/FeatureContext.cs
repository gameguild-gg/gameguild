namespace GameGuild.Modules.Features.Models;

/// <summary>
///     Advanced context for feature flag evaluation
/// </summary>
public class FeatureContext
{
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public string? SubscriptionPlanId { get; set; }

    public string Environment { get; set; } = "production";

    public Dictionary<string, object> CustomAttributes { get; init; } = new Dictionary<string, object>();

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public string? Country { get; set; }

    public DateTime RequestTime { get; set; } = DateTime.UtcNow;
}

