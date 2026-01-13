namespace GameGuild.Configuration.PresentationLayer.RateLimiting;

/// <summary>
///     Named rate limiting policies for the application.
///     Use these constants with [EnableRateLimiting] and [DisableRateLimiting] attributes.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>
    ///     Strict rate limiting for authentication endpoints (sign-in, sign-up, password reset).
    ///     Default: 10 requests per minute per client IP.
    ///     Purpose: Prevent brute-force attacks and credential stuffing.
    /// </summary>
    public const string Authentication = "authentication";

    /// <summary>
    ///     Rate limiting for authorization/permission check endpoints.
    ///     Default: 100 requests per minute per client IP.
    ///     Purpose: Prevent DoS attacks on permission evaluation.
    /// </summary>
    public const string Authorization = "authorization";

    /// <summary>
    ///     General API rate limiting for all other endpoints.
    ///     Default: 60 requests per minute per client IP.
    ///     Purpose: Fair usage and protection against abuse.
    /// </summary>
    public const string Api = "api";

    /// <summary>
    ///     Relaxed rate limiting for internal/admin endpoints.
    ///     Default: 200 requests per minute per client IP.
    ///     Purpose: Allow higher throughput for admin operations.
    /// </summary>
    public const string Internal = "internal";
}
