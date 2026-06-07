namespace GameGuild.Identity.Authentication;

/// <summary>
///     Authentication module configuration options
/// </summary>
public class AuthenticationModuleOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    ///     Enable or disable permission caching
    /// </summary>
    public bool EnablePermissionCaching { get; set; } = true;

    /// <summary>
    ///     Permission cache expiration time in minutes
    /// </summary>
    public int PermissionCacheExpirationMinutes { get; set; } = 30;

    /// <summary>
    ///     Enable or disable ABAC policy evaluation
    /// </summary>
    public bool EnableAbacPolicies { get; set; } = true;

    /// <summary>
    ///     Enable or disable conditional policies
    /// </summary>
    public bool EnableConditionalPolicies { get; set; } = true;

    /// <summary>
    ///     Enable or disable access review workflows
    /// </summary>
    public bool EnableAccessReviews { get; set; } = true;

    /// <summary>
    ///     Maximum number of policies to evaluate per request
    /// </summary>
    public int MaxPoliciesPerEvaluation { get; set; } = 100;

    /// <summary>
    ///     Enable detailed audit logging
    /// </summary>
    public bool EnableDetailedAuditLogging { get; set; } = true;

    /// <summary>
    ///     Enable performance metrics collection
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = true;
}
