namespace GameGuild.Configuration.PresentationLayer.RateLimiting;

/// <summary>
///     Configuration options for rate limiting policies.
///     Supports multiple partitioning strategies (IP, User, Tenant, API Key)
///     and multiple algorithms (Fixed Window, Sliding Window, Token Bucket, Concurrency).
/// </summary>
public sealed class RateLimitingOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "RateLimiting";

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
    ///     Partitioned by: IP address (anonymous) or User ID (authenticated).
    /// </summary>
    public int AuthenticationRequestsPerMinute { get; set; } = 10;

    /// <summary>
    ///     Authorization/permission check endpoint rate limit (requests per minute).
    ///     Default: 100 req/min for normal permission checks.
    ///     Partitioned by: User ID + Tenant ID.
    /// </summary>
    public int AuthorizationRequestsPerMinute { get; set; } = 100;

    /// <summary>
    ///     API endpoint rate limit (requests per minute).
    ///     Default: 60 req/min for general API calls.
    ///     Partitioned by: User ID (authenticated) or IP (anonymous).
    /// </summary>
    public int ApiRequestsPerMinute { get; set; } = 60;

    /// <summary>
    ///     Queue limit for requests that exceed the rate limit.
    ///     Requests beyond this are rejected immediately.
    /// </summary>
    public int QueueLimit { get; set; } = 2;

    // ============ Per-Tenant Rate Limiting ============

    /// <summary>
    ///     Per-tenant rate limit (requests per minute).
    ///     Default: 1000 req/min per tenant to prevent one tenant from impacting others.
    /// </summary>
    public int TenantRequestsPerMinute { get; set; } = 1000;

    // ============ Per-User Rate Limiting ============

    /// <summary>
    ///     Per-user rate limit (requests per minute).
    ///     Default: 300 req/min per authenticated user.
    /// </summary>
    public int UserRequestsPerMinute { get; set; } = 300;

    // ============ API Key Rate Limiting ============

    /// <summary>
    ///     Standard API key rate limit (requests per minute).
    ///     Default: 100 req/min for standard tier API keys.
    /// </summary>
    public int StandardApiKeyRequestsPerMinute { get; set; } = 100;

    /// <summary>
    ///     Premium API key rate limit (requests per minute).
    ///     Default: 1000 req/min for premium tier API keys.
    /// </summary>
    public int PremiumApiKeyRequestsPerMinute { get; set; } = 1000;

    // ============ Token Bucket Settings ============

    /// <summary>
    ///     Token bucket limit (max tokens).
    ///     Used for bursty traffic patterns.
    /// </summary>
    public int TokenBucketLimit { get; set; } = 100;

    /// <summary>
    ///     Token bucket replenishment period.
    /// </summary>
    public TimeSpan TokenReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Tokens added per replenishment period.
    /// </summary>
    public int TokensPerPeriod { get; set; } = 20;

    // ============ Concurrency Limiting ============

    /// <summary>
    ///     Maximum concurrent requests allowed.
    ///     Used for expensive operations (reports, exports).
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    public static RateLimitingOptions CreateDefault() { return new RateLimitingOptions(); }
}
