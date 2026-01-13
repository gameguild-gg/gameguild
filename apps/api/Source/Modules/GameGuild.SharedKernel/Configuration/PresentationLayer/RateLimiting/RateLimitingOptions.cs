namespace GameGuild.Configuration.PresentationLayer.RateLimiting;

/// <summary>
///     Configuration options for rate limiting policies.
/// </summary>
public class RateLimitingOptions : BaseOptions
{
    /// <summary>
    ///     Whether rate limiting is enabled globally.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = false;

    /// <summary>
    ///     Default limit for requests (used by global policy).
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    ///     Default time period for rate limiting.
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromMinutes(1);

    // Compatibility properties
    public int RequestsPerMinute { get; set; } = 60;

    public int BurstSize { get; set; } = 10;

    public string[] ExemptPaths { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Authentication endpoint rate limit (requests per minute).
    ///     Default: 10 req/min to prevent brute-force attacks.
    /// </summary>
    public int AuthenticationRequestsPerMinute { get; set; } = 10;

    /// <summary>
    ///     Authorization/permission check endpoint rate limit (requests per minute).
    ///     Default: 100 req/min for normal permission checks.
    /// </summary>
    public int AuthorizationRequestsPerMinute { get; set; } = 100;

    /// <summary>
    ///     API endpoint rate limit (requests per minute).
    ///     Default: 60 req/min for general API calls.
    /// </summary>
    public int ApiRequestsPerMinute { get; set; } = 60;

    /// <summary>
    ///     Queue limit for requests that exceed the rate limit.
    ///     Requests beyond this are rejected immediately.
    /// </summary>
    public int QueueLimit { get; set; } = 2;

    public static RateLimitingOptions CreateDefault() { return new RateLimitingOptions(); }
}
