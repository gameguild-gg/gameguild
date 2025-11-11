namespace GameGuild.Authentication.Extensions;

/// <summary>
///     Authentication module configuration options
/// </summary>
public class AuthenticationModuleOptions
{
    public const string SectionName = "Authentication";

    public bool EnablePermissionCaching { get; set; } = true;

    public int PermissionCacheExpirationMinutes { get; set; } = 30;

    public bool EnableAbacPolicies { get; set; } = true;

    public bool EnableConditionalPolicies { get; set; } = true;

    public bool EnableAccessReviews { get; set; } = true;

    public int MaxPoliciesPerEvaluation { get; set; } = 100;

    public bool EnableDetailedAuditLogging { get; set; } = true;

    public bool EnablePerformanceMetrics { get; set; } = true;

    public bool EnableAutoPermissionCleanup { get; set; } = true;

    public int PermissionCleanupIntervalDays { get; set; } = 30;

    public bool EnableSecurityEventMonitoring { get; set; } = true;
}
