namespace GameGuild.Identity.Tenants;

/// <summary>
///     Response of tenant usage validation
/// </summary>
public class TenantUsageValidationResponse
{
    public bool IsValid { get; init; }

    public Dictionary<string, UsageMetric> Metrics { get; init; } = new Dictionary<string, UsageMetric>();

    public IReadOnlyList<string> Violations { get; init; } = new List<string>();

    public bool RequiresUpgrade { get; init; }

    public DateTime ValidatedAt { get; init; } = SystemClock.UtcNow;

    /// <summary>
    ///     Creates a valid Response
    /// </summary>
    public static TenantUsageValidationResponse Valid(Dictionary<string, UsageMetric>? metrics = null)
    {
        return new TenantUsageValidationResponse { IsValid = true, Metrics = metrics ?? new Dictionary<string, UsageMetric>() };
    }

    /// <summary>
    ///     Creates an invalid Response with violations
    /// </summary>
    public static TenantUsageValidationResponse Invalid(IReadOnlyList<string> violations, Dictionary<string, UsageMetric>? metrics = null, bool requiresUpgrade = false)
    {
        return new TenantUsageValidationResponse { IsValid = false, Violations = violations, Metrics = metrics ?? new Dictionary<string, UsageMetric>(), RequiresUpgrade = requiresUpgrade };
    }
}
