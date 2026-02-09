using System.Threading.RateLimiting;
using GameGuild.Configuration;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.API;

/// <summary>
///     Extension methods for configuring rate limiting services and policies.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection SetupRateLimiting(this IServiceCollection services, IConfiguration configuration,
        RateLimitingOptions? options)
    {
        options ??= OptionBuilderUtilities.CreateAndBind(configuration, "RateLimiting",
            RateLimitingOptions.CreateDefault);
        options.Validate();

        services.AddRateLimiter(rateLimiterOptions =>
            {
                // Global rejection handler for rate limit exceeded
                rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/problem+json";

                    var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                        ? retryAfterValue.TotalSeconds
                        : 60;

                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Detail = $"Rate limit exceeded. Please retry after {retryAfter:F0} seconds.",
                        Instance = context.HttpContext.Request.Path
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
                };

                // ============ FIXED WINDOW POLICIES ============

                // Authentication policy: Partitioned by IP (anonymous) or User ID (authenticated)
                // 10 requests per minute to prevent brute-force attacks
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Authentication, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetAuthenticationPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.AuthenticationRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Authorization policy: Partitioned by User ID + Tenant ID
                // 100 requests per minute to prevent DoS on permission evaluation
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Authorization, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserTenantPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.AuthorizationRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Internal policy: Relaxed limits for admin/internal endpoints
                // Partitioned by User ID
                // 200 requests per minute
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Internal, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 200,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit * 2
                        }));

                // ============ SLIDING WINDOW POLICIES ============

                // API policy: General API endpoints with sliding window for smoother distribution
                // Partitioned by User ID (authenticated) or IP (anonymous)
                // 60 requests per minute for general API calls
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Api, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetUserOrIpPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.ApiRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4, // 4 segments = 15-second buckets
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-Tenant policy: Sliding window partitioned by Tenant ID
                // 1000 requests per minute per tenant
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerTenant, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetTenantPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.TenantRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-User policy: Sliding window partitioned by User ID
                // 300 requests per minute per authenticated user
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerUser, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = options.UserRequestsPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // Per-IP policy: Sliding window partitioned by IP address for anonymous protection
                // 30 requests per minute per IP
                rateLimiterOptions.AddPolicy(RateLimitPolicies.PerIp, httpContext =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: GetIpPartitionKey(httpContext),
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0 // No queuing for per-IP to prevent resource exhaustion
                        }));

                // ============ TOKEN BUCKET POLICIES ============

                // Bursty policy: Token bucket for bursty traffic patterns
                // Partitioned by User ID or IP
                rateLimiterOptions.AddPolicy(RateLimitPolicies.Bursty, httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: GetUserOrIpPartitionKey(httpContext),
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = options.TokenBucketLimit,
                            ReplenishmentPeriod = options.TokenReplenishmentPeriod,
                            TokensPerPeriod = options.TokensPerPeriod,
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // API Key policy: Token bucket partitioned by API key with tiered limits
                rateLimiterOptions.AddPolicy(RateLimitPolicies.ApiKey, httpContext =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: GetApiKeyPartitionKey(httpContext),
                        factory: partition => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = partition.StartsWith("premium:")
                                ? options.PremiumApiKeyRequestsPerMinute
                                : options.StandardApiKeyRequestsPerMinute,
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = partition.StartsWith("premium:")
                                ? options.PremiumApiKeyRequestsPerMinute
                                : options.StandardApiKeyRequestsPerMinute,
                            AutoReplenishment = true,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));

                // ============ CONCURRENCY POLICIES ============

                // Expensive Operations policy: Concurrency limiter for reports, exports, etc.
                // Partitioned by User ID to limit concurrent expensive operations per user
                rateLimiterOptions.AddPolicy(RateLimitPolicies.ExpensiveOperations, httpContext =>
                    RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: GetUserPartitionKey(httpContext),
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = options.MaxConcurrentRequests,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = options.QueueLimit
                        }));
            }
        );

        return services;
    }

    #region Rate Limiting Partition Key Helpers

    /// <summary>
    /// Gets partition key based on IP for anonymous or User ID for authenticated.
    /// Used for authentication endpoints.
    /// </summary>
    private static string GetAuthenticationPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on User ID only.
    /// Falls back to IP for anonymous users.
    /// </summary>
    private static string GetUserPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"anonymous:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on User ID + Tenant ID.
    /// Used for authorization/permission check endpoints.
    /// </summary>
    private static string GetUserTenantPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "default";

        return $"user:{userId}:tenant:{tenantId}";
    }

    /// <summary>
    /// Gets partition key based on User ID (authenticated) or IP (anonymous).
    /// Used for general API rate limiting.
    /// </summary>
    private static string GetUserOrIpPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on Tenant ID.
    /// Used for per-tenant rate limiting.
    /// </summary>
    private static string GetTenantPartitionKey(HttpContext httpContext)
    {
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            return $"tenant:{tenantId}";
        }

        // Fall back to IP for requests without tenant context
        return $"no-tenant:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on IP address only.
    /// Used for per-IP rate limiting (anonymous protection).
    /// </summary>
    private static string GetIpPartitionKey(HttpContext httpContext)
    {
        return $"ip:{GetClientIpAddress(httpContext)}";
    }

    /// <summary>
    /// Gets partition key based on API key from header.
    /// Returns tier prefix (premium: or standard:) for tiered limits.
    /// </summary>
    private static string GetApiKeyPartitionKey(HttpContext httpContext)
    {
        var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            // No API key - fall back to per-IP limiting
            return $"no-key:{GetClientIpAddress(httpContext)}";
        }

        // Check if this is a premium key (simple prefix check for now)
        // In production, this would check against a key registry
        if (apiKey.StartsWith("pk_", StringComparison.OrdinalIgnoreCase))
        {
            return $"premium:{apiKey}";
        }

        return $"standard:{apiKey}";
    }

    /// <summary>
    /// Gets the client IP address, handling proxies and load balancers.
    /// </summary>
    private static string GetClientIpAddress(HttpContext httpContext)
    {
        // Check X-Forwarded-For header for proxied requests
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first (original client)
            var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstIp))
            {
                return firstIp;
            }
        }

        // Check X-Real-IP header (common with nginx)
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to direct connection IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    #endregion
}
