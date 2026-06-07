namespace GameGuild.Features;

/// <summary>
///     Advanced context for feature flag evaluation
/// </summary>
public class FeatureContext
{
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }

    public string? SubscriptionPlanId { get; set; }

    public string Environment { get; set; } = "production";

    /// <summary>
    ///     User permissions for permission-based targeting
    /// </summary>
    public List<string> Permissions { get; set; } = [];

    public Dictionary<string, object> CustomAttributes { get; init; } = [];

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public string? Country { get; set; }

    public DateTime RequestTime { get; set; } = SystemClock.UtcNow;
}
